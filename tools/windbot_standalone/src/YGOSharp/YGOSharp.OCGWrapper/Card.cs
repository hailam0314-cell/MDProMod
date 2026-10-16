using YGOSharp.OCGWrapper.Enums;
using System.Data;

namespace YGOSharp.OCGWrapper
{
    public class Card
    {
        public struct CardData
        {
            public int Code;
            public int Alias;
            public long Setcode;
            public int Type;
            public int Level;
            public int Attribute;
            public int Race;
            public int Attack;
            public int Defense;
            public int LScale;
            public int RScale;
            public int LinkMarker;
        }

        public int Id { get; private set; }
        public int Ot { get; private set; }
        public int Alias { get; private set; }
        public long Setcode { get; private set; }
        public int Type { get; private set; }

        public int Level { get; private set; }
        public int LScale { get; private set; }
        public int RScale { get; private set; }
        public int LinkMarker { get; private set; }

        public int Attribute { get; private set; }
        public int Race { get; private set; }
        public int Attack { get; private set; }
        public int Defense { get; private set; }

        internal CardData Data { get; private set; }

        public static Card Get(int id)
        {
            return CardsManager.GetCard(id);
        }

        public bool HasType(CardType type)
        {
            return ((Type & (int)type) != 0);
        }

        public bool IsExtraCard()
        {
            return (HasType(CardType.Fusion) || HasType(CardType.Synchro) || HasType(CardType.Xyz) || HasType(CardType.Link));
        }

        internal Card(IDataRecord reader)
        {
            Init(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2),
                reader.GetInt64(3), reader.GetInt32(4), reader.GetInt32(5),
                reader.GetInt32(6), reader.GetInt32(7), reader.GetInt32(8), reader.GetInt32(9));
        }

        internal Card(int id, int ot, int alias, long setcode, int type, int levelInfo,
                      int race, int attribute, int attack, int defense)
        {
            Init(id, ot, alias, setcode, type, levelInfo, race, attribute, attack, defense);
        }

        private void Init(int id, int ot, int alias, long setcode, int type, int levelInfo,
                          int race, int attribute, int attack, int defense)
        {
            Id = id;
            Ot = ot;
            Alias = alias;
            Setcode = setcode;
            Type = type;

            Level = levelInfo & 0xff;
            LScale = (levelInfo >> 24) & 0xff;
            RScale = (levelInfo >> 16) & 0xff;

            Race = race;
            Attribute = attribute;
            Attack = attack;
            Defense = defense;

            if (HasType(CardType.Link))
            {
                LinkMarker = Defense;
                Defense = 0;
            }

            Data = new CardData()
            {
                Code = Id,
                Alias = Alias,
                Setcode = Setcode,
                Type = Type,
                Level = Level,
                Attribute = Attribute,
                Race = Race,
                Attack = Attack,
                Defense = Defense,
                LScale = LScale,
                RScale = RScale,
                LinkMarker = LinkMarker
            };
        }
    }
}
