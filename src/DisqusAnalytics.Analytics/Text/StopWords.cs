using System.Collections.Frozen;

namespace DisqusAnalytics.Analytics.Text;

/// <summary>
/// Palavras ignoradas nas estatísticas de frequência.
/// A lista é comparada sem acentos e em minúsculas, portanto "não",
/// "nao" e "NÃO" são tratadas como a mesma palavra.
/// </summary>
public static class StopWords
{
    private static readonly FrozenSet<string> Words = Build();

    /// <summary>
    /// Palavras de ligação usadas para não montar bigramas como
    /// "de que" ou "o episódio". Palavras com conteúdo ficam de fora,
    /// então "bom dia" e "melhor episódio" continuam válidos.
    /// </summary>
    private static readonly FrozenSet<string> Linkers = BuildLinkers();

    public static bool Contains(string word)
    {
        return Words.Contains(
            TextNormalization.RemoveDiacritics(word));
    }

    public static bool IsLinker(string word)
    {
        return Linkers.Contains(
            TextNormalization.RemoveDiacritics(word));
    }

    private static FrozenSet<string> BuildLinkers()
    {
        var linkers = new[]
        {
            "a", "à", "as", "às", "o", "os", "ao", "aos",
            "um", "uma", "uns", "umas",
            "de", "da", "das", "do", "dos", "dum", "duma",
            "em", "na", "nas", "no", "nos", "num", "numa",
            "por", "pela", "pelas", "pelo", "pelos",
            "para", "pra", "pras", "pro", "pros",
            "com", "sem", "sob", "sobre", "entre", "até",
            "e", "ou", "mas", "que", "se", "nem",
            "ele", "ela", "eles", "elas", "eu", "nós",
            "meu", "minha", "seu", "sua", "teu", "tua",
            "este", "esta", "esse", "essa", "isso", "isto",
            "aquele", "aquela", "aquilo"
        };

        return linkers
            .Select(TextNormalization.RemoveDiacritics)
            .ToFrozenSet(StringComparer.Ordinal);
    }

    private static FrozenSet<string> Build()
    {
        var words = new[]
        {
            // Artigos, preposições e contrações.
            "a", "à", "as", "às", "o", "os", "ao", "aos",
            "um", "uma", "uns", "umas",
            "de", "da", "das", "do", "dos", "dum", "duma",
            "em", "na", "nas", "no", "nos", "num", "numa",
            "por", "pela", "pelas", "pelo", "pelos",
            "para", "pra", "pro", "pras", "pros",
            "com", "sem", "sob", "sobre", "entre", "até", "após",
            "desde", "durante", "contra", "perante", "trás",
            "atrás", "dentro", "fora", "acima", "abaixo",
            "e", "ou", "mas", "porém", "todavia", "entretanto",
            "contudo", "porém", "pois", "porque", "que", "se",
            "embora", "conforme", "segundo", "como", "quando",
            "enquanto", "logo", "assim", "então", "portanto",

            // Pronomes.
            "eu", "tu", "ele", "ela", "nós", "vós", "eles", "elas",
            "me", "te", "se", "lhe", "lhes", "nos", "vos",
            "mim", "ti", "si", "comigo", "contigo", "consigo",
            "meu", "minha", "meus", "minhas", "teu", "tua",
            "teus", "tuas", "seu", "sua", "seus", "suas",
            "nosso", "nossa", "nossos", "nossas",
            "dele", "dela", "deles", "delas",
            "este", "esta", "estes", "estas", "esse", "essa",
            "esses", "essas", "aquele", "aquela", "aqueles",
            "aquelas", "aquilo", "isto", "isso", "aquilo",
            "nesse", "nessa", "nesses", "nessas",
            "neste", "nesta", "nestes", "nestas",
            "naquele", "naquela", "naqueles", "naquelas",
            "disso", "disto", "nisso", "nisto",
            "daquele", "daquela", "daquilo",
            "quem", "qual", "quais", "quanto", "quanta",
            "quantos", "quantas", "onde", "aonde", "cujo", "cuja",
            "cujos", "cujas", "algo", "alguém", "ninguém",
            "tudo", "nada", "todos", "todas", "todo", "toda",
            "outro", "outra", "outros", "outras", "cada",
            "qualquer", "quaisquer", "vários", "várias",
            "ambos", "ambas", "mesmo", "mesma", "mesmos", "mesmas",
            "tal", "tais", "você", "vocês", "vcs", "vc",

            // Verbos e formas auxiliares.
            "ser", "sou", "é", "era", "eram", "foi", "foram",
            "fomos", "seja", "sejam", "sendo", "sido",
            "são", "será", "serão", "seria", "seriam",
            "fosse", "fossem", "forem", "tenha", "tenham",
            "tiver", "tivesse", "tiveram",
            "estar", "estou", "está", "estão", "estava",
            "estavam", "estive", "estiver", "estado",
            "ter", "tenho", "tem", "temos", "têm", "tinha",
            "tinham", "tive", "teve", "tido", "tendo",
            "haver", "há", "havia", "houve",
            "ir", "vou", "vai", "vamos", "vão", "fui", "foi",
            "ia", "iam", "indo",
            "fazer", "faço", "faz", "fazem", "fiz", "fez",
            "fizeram", "feito", "fazendo",
            "dar", "dou", "dá", "dão", "deu", "deram", "dado",
            "ver", "vejo", "vê", "veem", "vi", "viu", "visto",
            "dizer", "digo", "diz", "dizem", "disse", "disseram",
            "falar", "falo", "fala", "falam", "falei", "falou",
            "poder", "posso", "pode", "podem", "pude", "pôde",
            "conseguir", "consigo", "consegue", "consegui",
            "querer", "quero", "quer", "querem", "quis",
            "saber", "sei", "sabe", "sabem", "soube",
            "achar", "acho", "acha", "acham", "achei", "achou",
            "olhar", "olha", "olhem", "olhei", "olhou",
            "ficar", "fico", "fica", "ficam", "fiquei", "ficou",
            "levar", "leva", "levou", "pegar", "pega", "pegou",
            "passar", "passa", "passou", "chegar", "chega", "chegou",
            "tô", "tá", "tão", "tava", "tavam", "tô",

            // Advérbios e conectivos comuns.
            "não", "nao", "sim", "já", "ainda", "agora", "antes",
            "depois", "sempre", "nunca", "jamais", "muito",
            "muita", "muitos", "muitas", "pouco", "pouca",
            "bem", "mal", "mais", "menos", "melhor", "pior",
            "aqui", "ali", "lá", "acolá", "aí", "daí",
            "apenas", "somente", "também", "tão", "tanto",
            "tanta", "tantos", "tantas", "quase", "talvez",
            "hoje", "ontem", "amanhã", "sempre", "enfim",
            "então", "tipo", "coisa", "coisas", "gente",
            "cara", "mano", "né", "ne", "eh", "ah", "oh", "hmm",
            "ai", "aí", "ue", "ué", "opa", "eita", "nossa",
            "porra", "merda", "caramba", "poxa", "puts",

            // Gírias e abreviações de internet.
            "kkk", "kkkk", "kkkkk", "kkkkkk", "rs", "rsrs",
            "rsrsrs", "haha", "hahaha", "hehe", "hehehe",
            "lol", "huahua", "hue", "huehue", "sqn", "tbm",
            "tb", "tmb", "pq", "pqp", "q", "vc", "vcs", "mto",
            "mt", "blz", "flw", "vlw", "obg", "abs", "bjs",

            // Inglês.
            "the", "and", "you", "your", "yours", "that", "this",
            "these", "those", "there", "here", "what", "which",
            "who", "whom", "whose", "when", "where", "why", "how",
            "for", "with", "without", "from", "into", "about",
            "over", "under", "again", "then", "than", "too",
            "very", "just", "only", "also", "not", "nor", "but",
            "all", "any", "both", "each", "few", "more", "most",
            "other", "some", "such", "own", "same", "so", "out",
            "up", "down", "off", "on", "in", "at", "by", "as",
            "is", "are", "was", "were", "be", "been", "being",
            "am", "do", "does", "did", "doing", "done", "have",
            "has", "had", "having", "i", "he", "she", "it", "we",
            "they", "me", "him", "her", "us", "them", "my", "his",
            "its", "our", "their", "can", "will", "would", "could",
            "should", "may", "might", "must", "shall", "get", "got",
            "like", "know", "think", "really", "well", "yeah",
            "yes", "ok", "okay", "omg", "please", "thanks"
        };

        return words
            .Select(TextNormalization.RemoveDiacritics)
            .Select(word => word.Trim().ToLowerInvariant())
            .Where(word => word.Length > 0)
            .ToFrozenSet(StringComparer.Ordinal);
    }
}
