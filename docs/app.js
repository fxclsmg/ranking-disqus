"use strict";

const ACCENTS = ["amber", "teal", "violet", "rose"];
const MONTHS_SHORT = [
  "jan", "fev", "mar", "abr", "mai", "jun",
  "jul", "ago", "set", "out", "nov", "dez"
];
const MONTHS_LONG = [
  "janeiro", "fevereiro", "março", "abril", "maio", "junho",
  "julho", "agosto", "setembro", "outubro", "novembro", "dezembro"
];
const WEEKDAYS = {
  0: "dom", 1: "seg", 2: "ter", 3: "qua", 4: "qui", 5: "sex", 6: "sáb"
};
const WEEKDAY_ORDER = [1, 2, 3, 4, 5, 6, 0];

const numberFormat = new Intl.NumberFormat("pt-BR");
const decimalFormat = new Intl.NumberFormat("pt-BR", {
  minimumFractionDigits: 1,
  maximumFractionDigits: 1
});

/* ---------- utilidades ---------- */

function decodeEntities(value) {
  const area = document.createElement("textarea");
  area.innerHTML = value ?? "";
  return area.value;
}

function esc(value) {
  return String(value ?? "").replace(/[&<>"']/g, (char) => ({
    "&": "&amp;",
    "<": "&lt;",
    ">": "&gt;",
    '"': "&quot;",
    "'": "&#39;"
  }[char]));
}

function text(value) {
  return esc(decodeEntities(value));
}

function slugify(value) {
  return String(value ?? "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-|-$/g, "");
}

const TIME_ZONE = "America/Sao_Paulo";

function formatDate(iso) {
  if (!iso) return "—";
  const date = new Date(iso);
  return Number.isNaN(date.getTime())
    ? "—"
    : date.toLocaleDateString("pt-BR", { timeZone: TIME_ZONE });
}

function formatDateTime(iso) {
  if (!iso) return "—";
  const date = new Date(iso);
  return Number.isNaN(date.getTime())
    ? "—"
    : date.toLocaleString("pt-BR", {
        timeZone: TIME_ZONE,
        day: "2-digit",
        month: "long",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit"
      });
}

function formatMonth(period) {
  const [year, month] = String(period ?? "").split("-");
  const name = MONTHS_SHORT[Number(month) - 1];
  return name ? `${name}/${year.slice(2)}` : period;
}

function formatMonthLong(period) {
  const [year, month] = String(period ?? "").split("-");
  const name = MONTHS_LONG[Number(month) - 1];
  return name ? `${name} de ${year}` : period;
}

function formatDuration(hours) {
  const value = Number(hours) || 0;

  if (value <= 0) return "—";
  if (value < 1) return `${Math.round(value * 60)} min`;
  if (value < 48) return `${decimalFormat.format(value)} h`;

  return `${decimalFormat.format(value / 24)} dias`;
}

/* ---------- gráfico de barras (SVG, sem dependências) ---------- */

/**
 * Tamanho do texto dos gráficos em pixels de tela. O SVG é desenhado
 * com uma largura fixa e depois reduzido para caber no painel; sem o
 * ajuste, o texto do gráfico em painel estreito fica menor.
 */
const CHART_TEXT_PX = 13;

function fitChartText(svg) {
  const rendered = svg.getBoundingClientRect().width;

  if (!rendered) {
    return;
  }

  const viewWidth = svg.viewBox.baseVal?.width || 820;

  svg.style.fontSize =
    `${(CHART_TEXT_PX * viewWidth / rendered).toFixed(2)}px`;
}

function fitCharts(root) {
  (root ?? document)
    .querySelectorAll("svg[data-chart-svg]")
    .forEach(fitChartText);
}

function drawBars(canvas, items) {
  canvas.innerHTML = buildBarsSvg(items);

  fitCharts(canvas);
}

function buildBarsSvg(items) {
  const width = 820;
  const height = 280;  const margin = { top: 16, right: 16, bottom: 36, left: 52 };
  const innerWidth = width - margin.left - margin.right;
  const innerHeight = height - margin.top - margin.bottom;

  const max = Math.max(1, ...items.map((item) => item.value));
  const band = innerWidth / items.length;
  const barWidth = Math.max(1.5, band * 0.66);

  const yOf = (value) =>
    margin.top + innerHeight - (value / max) * innerHeight;

  const parts = [];

  const steps = 4;
  for (let step = 0; step <= steps; step += 1) {
    const value = (max / steps) * step;
    const y = yOf(value);

    parts.push(
      `<line class="grid-line" x1="${margin.left}" y1="${y.toFixed(1)}" ` +
      `x2="${(width - margin.right).toFixed(1)}" y2="${y.toFixed(1)}"/>`
    );

    parts.push(
      `<text class="axis-label" x="${margin.left - 8}" y="${(y + 4).toFixed(1)}" ` +
      `text-anchor="end">${numberFormat.format(Math.round(value))}</text>`
    );
  }

  const labelEvery = Math.max(1, Math.ceil(items.length / 12));

  items.forEach((item, index) => {
    const x = margin.left + index * band + (band - barWidth) / 2;
    const y = yOf(item.value);
    const barHeight = Math.max(0, margin.top + innerHeight - y);

    parts.push(
      `<rect class="bar" x="${x.toFixed(1)}" y="${y.toFixed(1)}" ` +
      `width="${barWidth.toFixed(1)}" height="${barHeight.toFixed(1)}" rx="2">` +
      `<title>${esc(item.tooltip)}</title></rect>`
    );

    if (index % labelEvery === 0) {
      parts.push(
        `<text class="axis-label" x="${(x + barWidth / 2).toFixed(1)}" ` +
        `y="${height - 12}" text-anchor="middle">${esc(item.label)}</text>`
      );
    }
  });

  return `<svg viewBox="0 0 ${width} ${height}" role="img" ` +
    `aria-label="Gráfico de barras" font-size="${CHART_TEXT_PX}" ` +
    `data-chart-svg>${parts.join("")}</svg>`;
}

/**
 * O relatório só lista os meses que tiveram comentário. Para o gráfico
 * representar o tempo linearmente, os meses sem movimento entram como zero.
 */
function fillMonths(points) {
  if (!points.length) {
    return [];
  }

  const ordered = [...points].sort((a, b) =>
    a.period < b.period ? -1 : a.period > b.period ? 1 : 0
  );

  const byPeriod = new Map(ordered.map((point) => [point.period, point]));

  const [startYear, startMonth] = ordered[0].period.split("-").map(Number);
  const [endYear, endMonth] = ordered[ordered.length - 1].period
    .split("-")
    .map(Number);

  const result = [];
  let year = startYear;
  let month = startMonth;

  while (year < endYear || (year === endYear && month <= endMonth)) {
    const period = `${year}-${String(month).padStart(2, "0")}`;
    const point = byPeriod.get(period);

    result.push({
      period,
      comments: point?.comments ?? 0,
      authors: point?.authors ?? 0
    });

    month += 1;

    if (month > 12) {
      month = 1;
      year += 1;
    }
  }

  return result;
}

function monthlyItems(series, key) {
  return series.map((point) => ({
    label: formatMonth(point.period),
    value: key === "authors" ? point.authors : point.comments,
    tooltip:
      `${formatMonthLong(point.period)} — ` +
      `${numberFormat.format(point.comments)} comentários, ` +
      `${numberFormat.format(point.authors)} autores`
  }));
}

function mountChart(container, points) {
  const series = fillMonths(points ?? []);

  if (!series.length) {
    container.innerHTML = `<p class="hint">Sem dados para o período.</p>`;
    return;
  }

  container.innerHTML = `
    <div class="series-toggle" role="group" aria-label="Série do gráfico">
      <button type="button" class="toggle" data-series="comments">Comentários</button>
      <button type="button" class="toggle" data-series="authors">Autores</button>
    </div>
    <div class="chart-canvas"></div>
  `;

  const canvas = container.querySelector(".chart-canvas");
  const buttons = [...container.querySelectorAll(".toggle")];

  const draw = (key) => {
    drawBars(canvas, monthlyItems(series, key));
    buttons.forEach((button) => {
      button.setAttribute(
        "aria-pressed",
        String(button.dataset.series === key)
      );
    });
  };

  buttons.forEach((button) => {
    button.addEventListener("click", () => draw(button.dataset.series));
  });

  draw("comments");
}

/* ---------- tamanho das palavras ---------- */

const WORD_BANDS = [
  { label: "3–5", min: 3, max: 5 },
  { label: "6–10", min: 6, max: 10 },
  { label: "11–15", min: 11, max: 15 },
  { label: "16+", min: 16, max: Number.MAX_SAFE_INTEGER }
];

function wordLengthBands(rows) {
  return WORD_BANDS.map((band) => {
    const inBand = rows.filter(
      (row) => row.length >= band.min && row.length <= band.max
    );

    const words = inBand.reduce((total, row) => total + row.words, 0);
    const occurrences = inBand.reduce(
      (total, row) => total + row.occurrences,
      0
    );
    const rareWords = inBand.reduce(
      (total, row) => total + row.rareWords,
      0
    );

    return {
      label: band.label,
      words,
      occurrences,
      rareWords,
      rareShare: words === 0 ? 0 : (rareWords * 100) / words
    };
  });
}

function mountLengthChart(container, rows) {
  const bands = wordLengthBands(rows ?? []);

  if (!bands.length || bands.every((band) => band.words === 0)) {
    container.innerHTML = `<p class="hint">Sem palavras para exibir.</p>`;
    return;
  }

  const series = {
    words: bands.map((band) => ({
      label: band.label,
      value: band.words,
      tooltip:
        `${band.label} letras — ${numberFormat.format(band.words)} ` +
        "palavras distintas"
    })),
    occurrences: bands.map((band) => ({
      label: band.label,
      value: band.occurrences,
      tooltip:
        `${band.label} letras — ${numberFormat.format(band.occurrences)} ` +
        `ocorrências (${numberFormat.format(band.rareWords)} palavras únicas)`
    })),
    rareShare: bands.map((band) => ({
      label: band.label,
      value: band.rareShare,
      tooltip:
        `${band.label} letras — ${decimalFormat.format(band.rareShare)}% ` +
        "das palavras da faixa aparecem uma única vez"
    }))
  };

  container.innerHTML = `
    <div class="series-toggle" role="group" aria-label="Série do gráfico">
      <button type="button" class="toggle" data-key="words">Palavras</button>
      <button type="button" class="toggle" data-key="occurrences">Ocorrências</button>
      <button type="button" class="toggle" data-key="rareShare">% raras</button>
    </div>
    <div class="chart-canvas"></div>
  `;

  const canvas = container.querySelector(".chart-canvas");
  const buttons = [...container.querySelectorAll(".toggle")];

  const draw = (key) => {
    drawBars(canvas, series[key]);

    buttons.forEach((button) => {
      button.setAttribute(
        "aria-pressed",
        String(button.dataset.key === key)
      );
    });
  };

  buttons.forEach((button) => {
    button.addEventListener("click", () => draw(button.dataset.key));
  });

  draw("words");
}

function hashtagsList(items) {
  if (!items || !items.length) {
    return `<p class="hint">Nenhuma hashtag nos comentários.</p>`;
  }

  return barList(
    items.map((item) => ({
      word: item.tag,
      count: item.count,
      authors: item.authors
    })),
    { limit: 15 }
  );
}

function emojisList(items) {
  if (!items || !items.length) {
    return `<p class="hint">Nenhum emoji nos comentários.</p>`;
  }

  return `
    <ol class="words emojis">
      ${items.slice(0, 15).map((item) => `
        <li title="${esc(item.count)} usos em ${esc(item.authors)} autores">
          <span class="word">${text(item.emoji)}</span>
          <span class="count">
            ${numberFormat.format(item.count)} usos ·
            ${numberFormat.format(item.authors)} autores
          </span>
        </li>
      `).join("")}
    </ol>
  `;
}

/* ---------- renovação do vocabulário ---------- */

function mountVocabularyChart(container, rows) {
  if (!rows || !rows.length) {
    container.innerHTML = `<p class="hint">Sem dados.</p>`;
    return;
  }

  const series = {
    words: rows.map((row) => ({
      label: String(row.year),
      value: row.words,
      tooltip:
        `${row.year} — ${numberFormat.format(row.words)} palavras em uso, ` +
        `${numberFormat.format(row.newWords)} novas`
    })),
    newWords: rows.map((row) => ({
      label: String(row.year),
      value: row.newWords,
      tooltip:
        `${row.year} — ${numberFormat.format(row.newWords)} palavras ` +
        "usadas pela primeira vez"
    })),
    lostWords: rows.map((row) => ({
      label: String(row.year),
      value: row.lostWords,
      tooltip:
        `${row.year} — ${numberFormat.format(row.lostWords)} palavras ` +
        "que apareceram no ano anterior e não apareceram neste"
    }))
  };

  container.innerHTML = `
    <div class="series-toggle" role="group" aria-label="Série do gráfico">
      <button type="button" class="toggle" data-key="words">Em uso</button>
      <button type="button" class="toggle" data-key="newWords">Novas</button>
      <button type="button" class="toggle" data-key="lostWords">Sumiram</button>
    </div>
    <div class="chart-canvas"></div>
  `;

  const canvas = container.querySelector(".chart-canvas");
  const buttons = [...container.querySelectorAll(".toggle")];

  const draw = (key) => {
    drawBars(canvas, series[key]);

    buttons.forEach((button) => {
      button.setAttribute(
        "aria-pressed",
        String(button.dataset.key === key)
      );
    });
  };

  buttons.forEach((button) => {
    button.addEventListener("click", () => draw(button.dataset.key));
  });

  draw("words");
}

function seasonWords(rows) {
  if (!rows || !rows.length) {
    return `<p class="hint">Sem palavras exclusivas.</p>`;
  }

  return `
    <div class="seasons">
      ${rows.map((row) => `
        <div class="season-row">
          <span class="season-year">${row.year}</span>
          <span class="chips">
            ${(row.seasonWords ?? []).map((word) => `
              <span class="chip" title="${esc(word.count)} uso(s) em ${esc(word.authors)} autor(es)">
                ${text(word.word)}
              </span>
            `).join("")}
          </span>
        </div>
      `).join("")}
    </div>
  `;
}

function mountYearChart(container, rows) {  if (!rows || !rows.length) {
    container.innerHTML = `<p class="hint">Sem dados.</p>`;
    return;
  }

  const series = {
    newAuthors: {
      items: rows.map((row) => ({
        label: String(row.year),
        value: row.newAuthors,
        tooltip:
          `${row.year} — ${numberFormat.format(row.newAuthors)} novos autores`
      }))
    },
    comments: {
      items: rows.map((row) => ({
        label: String(row.year),
        value: row.comments,
        tooltip:
          `${row.year} — ${numberFormat.format(row.comments)} comentários, ` +
          `${numberFormat.format(row.activeAuthors)} autores ativos`
      }))
    },
    retention: {
      items: rows.map((row) => ({
        label: String(row.year),
        value: row.retentionShare,
        tooltip:
          `${row.year} — ${decimalFormat.format(row.retentionShare)}% dos ` +
          "autores que estrearam no ano comentaram depois"
      }))
    }
  };

  container.innerHTML = `
    <div class="series-toggle" role="group" aria-label="Série do gráfico">
      <button type="button" class="toggle" data-key="newAuthors">Novos autores</button>
      <button type="button" class="toggle" data-key="comments">Comentários</button>
      <button type="button" class="toggle" data-key="retention">% que voltou</button>
    </div>
    <div class="chart-canvas"></div>
  `;

  const canvas = container.querySelector(".chart-canvas");
  const buttons = [...container.querySelectorAll(".toggle")];

  const draw = (key) => {
    drawBars(canvas, series[key].items);

    buttons.forEach((button) => {
      button.setAttribute(
        "aria-pressed",
        String(button.dataset.key === key)
      );
    });
  };

  buttons.forEach((button) => {
    button.addEventListener("click", () => draw(button.dataset.key));
  });

  draw("newAuthors");
}

/* ---------- blocos ---------- */

function card(label, value, hint, accent) {
  return `
    <div class="card${accent ? " accent" : ""}">
      <span class="label">${esc(label)}</span>
      <span class="value">${esc(value)}</span>
      ${hint ? `<span class="hint">${esc(hint)}</span>` : ""}
    </div>
  `;
}

function authorsTable(authors) {
  if (!authors.length) {
    return `<p class="hint">Nenhum autor com comentários.</p>`;
  }

  const maxShare = Math.max(...authors.map((author) => author.share), 0.1);

  const rows = authors.map((author, index) => {
    const width = Math.max(2, (author.share / maxShare) * 100);
    const profile = author.profileUrl
      ? `<a class="author-name" href="${esc(author.profileUrl)}" ` +
        `target="_blank" rel="noopener noreferrer">${text(author.name)}</a>`
      : `<span class="author-name">${text(author.name)}</span>`;

    return `
      <tr>
        <td class="rank">${index + 1}</td>
        <td>
          ${profile}
          ${author.username ? `<span class="author-user">@${esc(author.username)}</span>` : ""}
        </td>
        <td class="num">${numberFormat.format(author.comments)}</td>
        <td class="num">
          <span class="share">
            <span class="track"><span class="fill" style="width:${width.toFixed(1)}%"></span></span>
            ${decimalFormat.format(author.share)}%
          </span>
        </td>
        <td class="num">${numberFormat.format(author.averageCharacters)}</td>
        <td class="num">${numberFormat.format(author.discussions)}</td>
        <td class="num">${decimalFormat.format(author.episodeShare)}%</td>
      </tr>
    `;
  }).join("");

  return `
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th class="rank">#</th>
            <th>Autor</th>
            <th class="num">Comentários</th>
            <th class="num">% do total</th>
            <th class="num">Média chars</th>
            <th class="num">Episódios</th>
            <th class="num">% episódios</th>
          </tr>
        </thead>
        <tbody>${rows}</tbody>
      </table>
    </div>
  `;
}

function barList(items, options) {
  const list = items.slice(0, options.limit ?? 20);
  const max = Math.max(...list.map((item) => item.count), 1);

  return `
    <ol class="words">
      ${list.map((item) => {
        const width = Math.max(3, (item.count / max) * 100);
        return `
          <li title="${esc(item.count)} ocorrências em ${esc(item.authors)} autores">
            <span class="word">${text(item.word)}</span>
            <span class="track"><span class="fill" style="width:${width.toFixed(1)}%"></span></span>
            <span class="count">${numberFormat.format(item.count)} · ${numberFormat.format(item.authors)} autores</span>
          </li>
        `;
      }).join("")}
    </ol>
  `;
}

function wordsList(words) {
  if (!words.length) {
    return `<p class="hint">Sem palavras para exibir.</p>`;
  }

  return barList(words, { limit: 20 });
}

function phrasesList(phrases) {
  if (!phrases.length) {
    return `<p class="hint">Sem bigramas para exibir.</p>`;
  }

  const items = phrases.slice(0, 15).map((phrase) => ({
    word: phrase.phrase,
    count: phrase.count,
    authors: phrase.authors
  }));

  return `<ol class="words phrases">${
    items.map((item) => {
      const max = Math.max(...items.map((entry) => entry.count), 1);
      const width = Math.max(3, (item.count / max) * 100);
      return `
        <li title="${esc(item.count)} ocorrências em ${esc(item.authors)} autores">
          <span class="word">${text(item.word)}</span>
          <span class="track"><span class="fill" style="width:${width.toFixed(1)}%"></span></span>
          <span class="count">${numberFormat.format(item.count)} · ${numberFormat.format(item.authors)} autores</span>
        </li>
      `;
    }).join("")
  }</ol>`;
}

function rareWordsList(words) {
  if (!words.length) {
    return `<p class="hint">Sem palavras raras.</p>`;
  }

  return `
    <ul class="chips">
      ${words.map((word) => `
        <li title="aparece 1 vez, usada por 1 autor">${text(word.word)}</li>
      `).join("")}
    </ul>
  `;
}

function episodesList(episodes) {
  if (!episodes.length) {
    return `<p class="hint">Nenhum episódio com comentários.</p>`;
  }

  return `
    <ol class="episodes">
      ${episodes.map((episode, index) => {
        const title = text(episode.title);
        const label = episode.link
          ? `<a href="${esc(episode.link)}" target="_blank" rel="noopener noreferrer">${title}</a>`
          : title;

        return `
          <li>
            <span class="rank">${index + 1}</span>
            <span class="title">${label}</span>
            <span class="meta">${numberFormat.format(episode.authors)} autores</span>
            <span class="comments">${numberFormat.format(episode.comments)}</span>
          </li>
        `;
      }).join("")}
    </ol>
  `;
}

function cohortsTable(rows) {
  if (!rows || !rows.length) {
    return `<p class="hint">Sem dados.</p>`;
  }

  const lastYear = Math.max(...rows.map((row) => row.year));

  const body = rows.map((row) => {
    const retention = row.year === lastYear
      ? "—"
      : `${decimalFormat.format(row.retentionShare)}%`;

    return `
      <tr>
        <td class="rank">${row.year}</td>
        <td class="num">${numberFormat.format(row.newAuthors)}</td>
        <td class="num">${retention}</td>
        <td class="num">${numberFormat.format(row.newAuthorsComments)}</td>
        <td class="num">${numberFormat.format(row.activeAuthors)}</td>
      </tr>
    `;
  }).join("");

  return `
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th class="rank">Ano</th>
            <th class="num">Novos</th>
            <th class="num">Voltaram</th>
            <th class="num">Coment. novos</th>
            <th class="num">Ativos</th>
          </tr>
        </thead>
        <tbody>${body}</tbody>
      </table>
    </div>
  `;
}

function newcomersList(items) {
  if (!items || !items.length) {
    return `<p class="hint">Nenhum estreante no período.</p>`;
  }

  return `
    <ol class="newcomers">
      ${items.map((item, index) => {
        const name = item.profileUrl
          ? `<a href="${esc(item.profileUrl)}" target="_blank" rel="noopener noreferrer">${text(item.name)}</a>`
          : text(item.name);

        return `
          <li>
            <span class="rank">${index + 1}</span>
            <span class="title">
              ${name}
              <span class="meta">
                chegou em ${formatDate(item.firstCommentAt)} ·
                ${numberFormat.format(item.discussions)} episódios
              </span>
            </span>
            <span class="comments">${numberFormat.format(item.comments)}</span>
          </li>
        `;
      }).join("")}
    </ol>
  `;
}

/* ---------- recordes ---------- */

function commentRecord(record, label) {
  if (!record) {
    return `<p class="hint">Sem dados.</p>`;
  }

  const author = record.profileUrl
    ? `<a href="${esc(record.profileUrl)}" target="_blank" rel="noopener noreferrer">${text(record.authorName)}</a>`
    : text(record.authorName);

  const episode = record.discussionLink
    ? `<a href="${esc(record.discussionLink)}" target="_blank" rel="noopener noreferrer">${text(record.discussionTitle)}</a>`
    : text(record.discussionTitle);

  return `
    <div class="record">
      <span class="record-label">${esc(label)}</span>
      <span class="record-value">${numberFormat.format(record.characters)} caracteres</span>
      <p class="record-author">${author} · ${formatDateTime(record.createdAt)}</p>
      <p class="record-episode">${episode}</p>
      <blockquote>${text(record.excerpt)}</blockquote>
    </div>
  `;
}

function edgeCommenters(items) {
  if (!items || !items.length) {
    return `<p class="hint">Sem dados.</p>`;
  }

  return `
    <ol class="newcomers">
      ${items.slice(0, 8).map((item, index) => {
        const name = item.profileUrl
          ? `<a href="${esc(item.profileUrl)}" target="_blank" rel="noopener noreferrer">${text(item.name)}</a>`
          : text(item.name);

        return `
          <li>
            <span class="rank">${index + 1}</span>
            <span class="title">
              ${name}
              <span class="meta">${decimalFormat.format(item.share)}% dos episódios</span>
            </span>
            <span class="comments">${numberFormat.format(item.count)}</span>
          </li>
        `;
      }).join("")}
    </ol>
  `;
}

function heatmap(cells) {
  if (!cells.length) {
    return `<p class="hint">Sem dados de horário.</p>`;
  }

  const grid = Array.from({ length: 7 }, () => Array(24).fill(0));

  cells.forEach((cell) => {
    grid[cell.weekday][cell.hour] += cell.comments;
  });

  const max = Math.max(1, ...grid.flat());

  const head = Array.from({ length: 24 }, (_, hour) =>
    `<span class="hm-hour">${hour % 3 === 0 ? hour : ""}</span>`
  ).join("");

  const rows = WEEKDAY_ORDER.map((day) => {
    const cellsOfDay = Array.from({ length: 24 }, (_, hour) => {
      const value = grid[day][hour];
      const style = value === 0
        ? ""
        : ` style="background:var(--accent);opacity:${(0.12 + 0.88 * (value / max)).toFixed(2)}"`;

      return `<span class="hm-cell"${style} title="${WEEKDAYS[day]} ` +
        `${String(hour).padStart(2, "0")}h — ${numberFormat.format(value)} comentários"></span>`;
    }).join("");

    return `<div class="hm-row"><span class="hm-day">${WEEKDAYS[day]}</span>${cellsOfDay}</div>`;
  }).join("");

  return `
    <div class="heatmap">
      <div class="hm-row hm-head"><span class="hm-day"></span>${head}</div>
      ${rows}
    </div>
    <p class="hint">Cada célula é um dia da semana e uma hora; quanto mais forte, mais comentários.</p>
  `;
}

/* ---------- show ---------- */

function renderShow(show, index) {
  const accent = ACCENTS[index % ACCENTS.length];
  const totals = show.totals ?? {};
  const section = document.createElement("section");
  section.className = "show";
  section.id = `show-${slugify(show.name)}`;
  section.dataset.accent = accent;
  section.setAttribute("role", "tabpanel");
  section.setAttribute("aria-label", show.name);

  const period = `Comentários de ${formatDate(totals.firstCommentAt)} ` +
    `a ${formatDate(totals.lastCommentAt)}`;

  section.innerHTML = `
    <div class="show-head">
      <h2>${text(show.name)}</h2>
      <p class="forum">fórum <code>${esc(show.forum)}</code> · ${esc(period)}</p>
    </div>

    <div class="cards">
      ${card("Comentários", numberFormat.format(totals.comments ?? 0), null, true)}
      ${card("Autores únicos", numberFormat.format(totals.authors ?? 0))}
      ${card("Discussões", numberFormat.format(totals.discussions ?? 0))}
      ${card("Comentários / episódio", decimalFormat.format(totals.commentsPerDiscussion ?? 0))}
      ${card("Média por comentário", `${numberFormat.format(Math.round(totals.averageCharacters ?? 0))} chars`)}
      ${card(
        "Autores com 1 comentário",
        numberFormat.format(totals.singleCommentAuthors ?? 0),
        `${decimalFormat.format(totals.singleCommentAuthorsShare ?? 0)}% dos autores`
      )}
      ${card(
        "Top 10 autores",
        `${decimalFormat.format(totals.topTenAuthorsShare ?? 0)}%`,
        "dos comentários"
      )}
      ${card(
        "1º comentário",
        formatDuration(totals.medianFirstResponseHours),
        "mediana após o episódio"
      )}
      ${card("Vocabulário", numberFormat.format(totals.vocabularySize ?? 0), "palavras distintas")}
      ${card(
        "Repetidos",
        numberFormat.format(totals.duplicatedComments ?? 0),
        `${decimalFormat.format(totals.duplicatedCommentsShare ?? 0)}% do total`
      )}
      ${card("Com links", numberFormat.format(totals.commentsWithLinks ?? 0), "comentários")}
      ${card("Hashtags", numberFormat.format(totals.hashtagCount ?? 0), "usos")}
    </div>

    <div class="grid">
      <article class="panel full">
        <h3>Evolução mês a mês</h3>
        <p class="hint">Passe o mouse nas barras para ver os números do mês.</p>
        <div class="chart" data-chart></div>
      </article>

      <article class="panel full">
        <h3>Top autores</h3>
        <p class="hint">
          Ordenado por comentários. “Episódios” é quantas discussões diferentes
          o autor comentou; “% episódios” compara com o total do podcast.
        </p>
        <div data-authors></div>
      </article>

      <article class="panel">
        <h3>Palavras mais usadas</h3>
        <p class="hint">Sem artigos, preposições, pronomes e gírias.</p>
        <div data-words></div>
      </article>

      <article class="panel">
        <h3>Bigramas mais usados</h3>
        <p class="hint">Pares de palavras que aparecem juntos (“bom dia”).</p>
        <div data-bigrams></div>
      </article>

      <article class="panel">
        <h3>Tamanho das palavras</h3>
        <p class="hint">
          Vocabulário por faixa de letras. A última faixa (16+) é onde
          ficam as palavras coladas e as hashtags antigas.
        </p>
        <div class="chart" data-word-lengths></div>
      </article>

      <article class="panel">
        <h3>Hashtags</h3>
        <p class="hint">Como a comunidade marca os episódios e as piadas.</p>
        <div data-hashtags></div>
      </article>

      <article class="panel">
        <h3>Episódios mais comentados</h3>
        <p class="hint">Clique no título para abrir a thread.</p>
        <div data-episodes></div>
      </article>

      <article class="panel">
        <h3>Emojis</h3>
        <p class="hint">
          ${numberFormat.format(totals.emojiCount ?? 0)} usos em
          ${numberFormat.format(totals.commentsWithEmoji ?? 0)} comentários.
        </p>
        <div data-emojis></div>
      </article>

      <article class="panel full">
        <h3>Palavras raras</h3>
        <p class="hint">
          ${numberFormat.format(totals.rareWordCount ?? 0)} palavras usadas uma
          única vez. Aqui, as maiores.
        </p>
        <div data-rare></div>
      </article>

      <article class="panel full">
        <h3>Renovação do vocabulário</h3>
        <p class="hint">
          ${decimalFormat.format(totals.oneYearWordsShare ?? 0)}% das palavras
          aparecem em um único ano. Embaixo, as “gírias” de cada ano — as
          palavras usadas só ali.
        </p>
        <div class="chart" data-vocabulary></div>
        <div data-season-words></div>
      </article>

      <article class="panel full">
        <h3>Quando a galera comenta</h3>
        <p class="hint">Dia da semana × hora do comentário.</p>
        <div data-heatmap></div>
      </article>

      <article class="panel full">
        <h3>Novos comentaristas</h3>
        <p class="hint">
          Quem estreou em cada ano, quantos continuaram depois e quem
          chegou mais recentemente.
        </p>
        <div class="chart" data-new-authors></div>
        <div class="split">
          <div>
            <h4>Por ano de estreia</h4>
            <div data-cohorts></div>
          </div>
          <div>
            <h4 data-newcomers-title>Chegaram recentemente</h4>
            <div data-newcomers></div>
          </div>
        </div>
      </article>

      <article class="panel full">
        <h3>Recordes</h3>
        <p class="hint">
          Os extremos de tamanho e quem costuma abrir e fechar as conversas
          de cada episódio.
        </p>
        <div class="records">
          <div data-longest></div>
          <div data-shortest></div>
        </div>
        <div class="split">
          <div>
            <h4>Quem chega primeiro</h4>
            <div data-first></div>
          </div>
          <div>
            <h4>Quem fecha a conversa</h4>
            <div data-last></div>
          </div>
        </div>
      </article>
    </div>
  `;

  mountChart(section.querySelector("[data-chart]"), show.commentsByMonth ?? []);
  mountYearChart(
    section.querySelector("[data-new-authors]"),
    show.newAuthorsByYear ?? []
  );
  section.querySelector("[data-authors]").innerHTML =
    authorsTable((show.topAuthors ?? []).slice(0, 15));
  section.querySelector("[data-words]").innerHTML =
    wordsList(show.topWords ?? []);
  section.querySelector("[data-bigrams]").innerHTML =
    phrasesList(show.topBigrams ?? []);
  mountLengthChart(
    section.querySelector("[data-word-lengths]"),
    show.wordLengths ?? []
  );
  section.querySelector("[data-hashtags]").innerHTML =
    hashtagsList(show.hashtags ?? []);

  section.querySelector("[data-emojis]").innerHTML =
    emojisList(show.emojis ?? []);

  mountVocabularyChart(
    section.querySelector("[data-vocabulary]"),
    show.vocabularyByYear ?? []
  );

  section.querySelector("[data-season-words]").innerHTML =
    seasonWords(show.vocabularyByYear ?? []);
  section.querySelector("[data-episodes]").innerHTML =
    episodesList((show.topDiscussions ?? []).slice(0, 10));
  section.querySelector("[data-rare]").innerHTML =
    rareWordsList(show.rareWords ?? []);
  section.querySelector("[data-heatmap]").innerHTML =
    heatmap(show.activityByWeekdayHour ?? []);

  const newcomers = show.newcomers ?? [];
  const newestYear = newcomers.length
    ? Math.max(...newcomers.map((item) =>
        Number(String(item.firstCommentAt).slice(0, 4))))
    : null;

  section.querySelector("[data-newcomers-title]").textContent =
    newestYear ? `Chegaram em ${newestYear}` : "Chegaram recentemente";
  section.querySelector("[data-newcomers]").innerHTML =
    newcomersList(newcomers);
  section.querySelector("[data-cohorts]").innerHTML =
    cohortsTable(show.newAuthorsByYear ?? []);

  section.querySelector("[data-longest]").innerHTML =
    commentRecord(show.longestComment, "Maior comentário");
  section.querySelector("[data-shortest]").innerHTML =
    commentRecord(show.shortestComment, "Menor comentário");
  section.querySelector("[data-first]").innerHTML =
    edgeCommenters(show.firstCommenters ?? []);
  section.querySelector("[data-last]").innerHTML =
    edgeCommenters(show.lastCommenters ?? []);

  return section;
}

/* ---------- navegação ---------- */

function activate(slug, buttons, sections, updateHash) {
  buttons.forEach((button) => {
    button.setAttribute(
      "aria-selected",
      String(button.dataset.target === slug)
    );
  });

  sections.forEach((section) => {
    section.classList.toggle("active", section.id === `show-${slug}`);
  });

  // Os gráficos só têm largura depois de a seção ficar visível.
  requestAnimationFrame(() => fitCharts());

  if (updateHash) {
    try {
      history.replaceState(null, "", `#${slug}`);
    } catch {
      // Em file:// alguns navegadores bloqueiam history.replaceState.
    }
  }
}

function renderTabs(shows, sections) {
  const tabs = document.getElementById("tabs");
  tabs.innerHTML = "";

  const buttons = shows.map((show, index) => {
    const slug = slugify(show.name);
    const button = document.createElement("button");

    button.type = "button";
    button.className = "tab";
    button.dataset.target = slug;
    button.style.setProperty(
      "--tab-accent",
      `var(--show-accent-${ACCENTS[index % ACCENTS.length]})`
    );
    button.setAttribute("role", "tab");
    button.setAttribute("aria-selected", "false");
    button.innerHTML = `<span class="dot"></span>${text(show.name)}`;

    button.addEventListener("click", () => {
      activate(slug, buttons, sections, true);
    });

    tabs.appendChild(button);
    return button;
  });

  const requested = location.hash.replace("#", "");
  const initial = shows.some((show) => slugify(show.name) === requested)
    ? requested
    : slugify(shows[0].name);

  activate(initial, buttons, sections, false);
}

/* ---------- carga ---------- */

function showError(error) {
  const template = document.getElementById("error-template");
  const node = template.content.cloneNode(true);

  node.querySelector(".message-detail").textContent =
    error instanceof Error ? error.message : String(error);

  document.getElementById("shows").appendChild(node);
  document.getElementById("page-subtitle").textContent =
    "Falha ao carregar os dados";
}

/**
 * Os dados chegam por statistics.js (window.DISQUS_STATS), o que
 * funciona inclusive com a página aberta via file://.
 * Se o script não estiver presente, tenta o JSON por fetch.
 */
async function loadReport() {
  if (window.DISQUS_STATS) {
    return window.DISQUS_STATS;
  }

  const response = await fetch("statistics.json", { cache: "no-store" });

  if (!response.ok) {
    throw new Error(`HTTP ${response.status} ao buscar statistics.json`);
  }

  return await response.json();
}

async function init() {
  const data = await loadReport();

  const shows = (data.shows ?? []).filter(Boolean);

  if (!shows.length) {
    throw new Error("O relatório não possui nenhum podcast.");
  }

  const title = shows.map((show) => decodeEntities(show.name)).join(" & ");
  document.title = `${title} — Ranking Disqus`;
  document.getElementById("page-title").textContent = title;

  const totals = shows.reduce(
    (accumulator, show) => {
      const showTotals = show.totals ?? {};
      accumulator.comments += showTotals.comments ?? 0;
      accumulator.authors += showTotals.authors ?? 0;
      accumulator.discussions += showTotals.discussions ?? 0;
      return accumulator;
    },
    { comments: 0, authors: 0, discussions: 0 }
  );

  document.getElementById("page-subtitle").textContent =
    `${numberFormat.format(totals.comments)} comentários de ` +
    `${numberFormat.format(totals.authors)} autores em ` +
    `${numberFormat.format(totals.discussions)} discussões · ` +
    `atualizado em ${formatDateTime(data.generatedAt)}`;

  document.getElementById("footer-meta").textContent =
    `Relatório gerado em ${formatDateTime(data.generatedAt)} · ` +
    `${shows.length} podcasts · dados do fórum ${shows[0].forum}.`;

  const container = document.getElementById("shows");
  container.innerHTML = "";

  const sections = shows.map((show, index) => {
    const section = renderShow(show, index);
    container.appendChild(section);
    return section;
  });

  renderTabs(shows, sections);

  window.addEventListener("resize", () => fitCharts());
}

init().catch(showError);
