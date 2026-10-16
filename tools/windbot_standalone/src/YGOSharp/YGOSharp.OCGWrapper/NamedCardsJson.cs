using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace YGOSharp.OCGWrapper
{
    /**
     * 从 MDProMod 生成的 cards.json 读卡名/数值，替代 WindBot 原来的 sqlite 加载。
     * 字段：name / desc / ot / alias / setcode / type / atk / def / level / race / attribute
     */
    public static class NamedCardsJson
    {
        public static int Load(string path)
        {
            var cards = new Dictionary<int, NamedCard>();
            using (FileStream stream = File.OpenRead(path))
            using (JsonDocument document = JsonDocument.Parse(stream))
            {
                foreach (JsonProperty entry in document.RootElement.EnumerateObject())
                {
                    if (!int.TryParse(entry.Name, out int id)) continue;
                    JsonElement value = entry.Value;
                    cards[id] = new NamedCard(
                        id,
                        GetInt(value, "ot"),
                        GetInt(value, "alias"),
                        GetLong(value, "setcode"),
                        GetInt(value, "type"),
                        GetInt(value, "level"),
                        GetInt(value, "race"),
                        GetInt(value, "attribute"),
                        GetInt(value, "atk"),
                        GetInt(value, "def"),
                        GetString(value, "name"),
                        GetString(value, "desc"));
                }
            }
            NamedCardsManager.SetCards(cards);
            return cards.Count;
        }

        private static int GetInt(JsonElement element, string name)
        {
            return element.TryGetProperty(name, out JsonElement value) && value.TryGetInt32(out int result)
                ? result : 0;
        }

        private static long GetLong(JsonElement element, string name)
        {
            return element.TryGetProperty(name, out JsonElement value) && value.TryGetInt64(out long result)
                ? result : 0L;
        }

        private static string GetString(JsonElement element, string name)
        {
            return element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty : string.Empty;
        }
    }
}
