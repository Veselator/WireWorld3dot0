namespace WireWorld3dot0
{
    public abstract class Tile
    {
        public TileType type { get; init; }

        protected TileDirection _direction;
        protected static readonly Dictionary<TileDirection, Point> _vectorDirection = new Dictionary<TileDirection, Point>()
        {
            { TileDirection.Top, new Point(0, -1) },
            { TileDirection.Bottom, new Point(0, 1) },
            { TileDirection.Left, new Point(-1, 0) },
            { TileDirection.Right, new Point(1, 0) },
            { TileDirection.NoDirection, new Point(0, 0) }
        };
        protected Point _currentDirection;

        protected static readonly Point[] _neighbours_directions = {
            new Point(-1, 0),
            new Point(1, 0),
            new Point(0, -1),
            new Point(0, 1)
        };
        protected Point[] _neighbours;

        public TileDirection direction
        {
            get => _direction;
            set
            {
                _direction = value;
                _currentDirection = _vectorDirection[value];
            }
        }

        public Point position;
        public bool isActive { get; set; }
        public TileMatrix tileMatrix;

        protected Dictionary<TileDirection, char> _tileCharacters;// = new Dictionary<TileDirection, char>();
        protected Dictionary<TileDirection, char> _tileCharactersIfActive;// = new Dictionary<TileDirection, char>();

        public Tile(TileDirection direction, Point position, bool isActive, TileMatrix tileMatrix)
        {
            _tileCharacters = new Dictionary<TileDirection, char>();
            _tileCharactersIfActive = new Dictionary<TileDirection, char>();
            this.position = position;
            _fillDirections();

            this.direction = direction;
            this.isActive = isActive;
            this.tileMatrix = tileMatrix;
        }

        public Tile(TileDirection direction, Point position, bool isActive, TileMatrix tileMatrix, string directionsIfNotActive, string directionsIfActive)
        {
            _tileCharacters = new Dictionary<TileDirection, char>();
            _tileCharactersIfActive = new Dictionary<TileDirection, char>();
            this.position = position;
            _fillDirections();
            _setCharacters(directionsIfNotActive, directionsIfActive);

            this.direction = direction;
            this.isActive = isActive;
            this.tileMatrix = tileMatrix;
        }

        // Стандартний конструктор
        public Tile()
        {
            this.position = new Point(1, 5);
            _fillDirections();

            this._direction = TileDirection.Top;
            this.isActive = false;
        }

        protected static Point invertDirection(Point direction)
        {
            return new Point(-direction.X, -direction.Y);
        }

        protected void _setCharacters(string directionsIfNotActive, string directionsIfActive)
        {
            // Проходимось по всім напрямкам та задаємо для них відповідні символи
            // Enum.GetValues<TileDirection>().Length - 1 адже останній напрям в enum TileDirection - це відсутність напрямку
            int i = 0;
            int notActiveDirsLength = directionsIfNotActive.Length;
            int activeDirsLength = directionsIfActive.Length;
            foreach (TileDirection currentDirection in Enum.GetValues<TileDirection>())
            {
                // Якщо напрямок невизначений, то пропускаємо
                if (currentDirection == TileDirection.NoDirection) continue;

                // Заповнуємо відповідні словники значеннями
                if (notActiveDirsLength != 0) _tileCharacters.Add(currentDirection, directionsIfNotActive[i]);
                if (activeDirsLength != 0) _tileCharactersIfActive.Add(currentDirection, directionsIfActive[i]);

                i++;
            }
        }

        protected void _fillDirections()
        {
            // Визначає координати сусідів
            _neighbours = new Point[4];
            int i = 0;
            foreach (Point neighbourDirection in _neighbours_directions)
            {
                _neighbours[i] = new Point(position.X + neighbourDirection.X, position.Y + neighbourDirection.Y);
                i++;
            }
        }

        // Функція, яка повертає віхдні сигнали
        protected bool[] getInputSignals()
        {
            Tile neighbourTile;
            Point neighbour;
            Point neighbourDirection;
            bool[] neighboursSignals = new bool[_neighbours_directions.Length];

            for (int i = 0; i < _neighbours_directions.Length; i++)
            {
                neighbourDirection = _neighbours_directions[i];
                if (neighbourDirection == _currentDirection) { continue; }
                neighbour = _neighbours[i];

                neighbourTile = tileMatrix.getTileAt(neighbour);

                if (neighbourTile.type == TileType.Undefined || neighbourTile.type == TileType.Empty) 
                {
                    continue; 
                }

                if (neighbourTile.isActive)
                {
                    neighboursSignals[i] = isNeighbourRelatedToUs(neighbourDirection, neighbourTile.direction);
                }
            }

            return neighboursSignals;
        }

        protected void getInputSignals(ref int trueSignalsCount, ref int totalSignals)
        {
            bool[] neighboursSignals = new bool[_neighbours_directions.Length];
            for (int i = 0; i < neighboursSignals.Length; i++)
            {
                // Пропускаем направление, куда указывает наш тайл
                if (_neighbours_directions[i] == _currentDirection) continue;

                // Проверяем, есть ли входящий сигнал (true или false)
                Tile neighbourTile = tileMatrix.getTileAt(_neighbours[i]);

                if (neighbourTile.type != TileType.Undefined && neighbourTile.type != TileType.Empty)
                {
                    if (isNeighbourRelatedToUs(_neighbours_directions[i], neighbourTile.direction))
                    {
                        totalSignals++;
                        if (neighbourTile.isActive) trueSignalsCount++;
                    }
                }
            }
        }

        public abstract void tick(); // Абстрактна функція для реалізації логіки блоку
        public abstract char getCharacter();

        protected bool isNeighbourRelatedToUs(Point neighbourRelativePosition, TileDirection neighbourDirection)
        {
            if (neighbourRelativePosition == _vectorDirection[TileDirection.Bottom] && neighbourDirection == TileDirection.Top) return true;
            if (neighbourRelativePosition == _vectorDirection[TileDirection.Left] && neighbourDirection == TileDirection.Right) return true;
            if (neighbourRelativePosition == _vectorDirection[TileDirection.Top] && neighbourDirection == TileDirection.Bottom) return true;
            if (neighbourRelativePosition == _vectorDirection[TileDirection.Right] && neighbourDirection == TileDirection.Left) return true;
            return false;
        }

        public override string ToString()
        {
            return getCharacter().ToString();
        }
    }

    public class UndefinedTile : Tile
    {
        public UndefinedTile()
        {
            this.type = TileType.Undefined;
        }

        public override void tick()
        {
            // Невизначений тайл не може мати реалізації
        }

        public override char getCharacter()
        {
            return ' ';
        }
    }

    public class EmptyTile : Tile
    {
        public EmptyTile(Point position, TileMatrix tileMatrix) : base(TileDirection.NoDirection, position, false, tileMatrix)
        {
            this.type = TileType.Empty;
        }

        public override void tick()
        {
            
        }

        public override char getCharacter()
        {
            return 'a';//tileCharacters[(int)direction];
        }
    }

    public class HoldTile : Tile
    {
        private bool isHoldedSignal;
        public HoldTile(TileDirection direction, Point position, bool isActive, TileMatrix tileMatrix) : base(direction, position, isActive, tileMatrix, "ншщз", "НШЩЗ")
        {
            type = TileType.Arrow;
        }

        public override void tick()
        {
            // Логіка стрілочки
            // Паттерн проектування такий, що кожна стрілочка
            // Може змінювати лише свій стан
            bool[] neighboursSignals = getInputSignals();
            if(neighboursSignals.Any(x => x))
            {
                if (!isHoldedSignal)
                {
                    isHoldedSignal = true;
                    return;
                }
                isHoldedSignal = false;
                isActive = true;
            }
            else
            {
                isHoldedSignal = false;
                isActive = false;
            }
        }

        public override char getCharacter()
        {
            return isActive ? _tileCharactersIfActive[_direction] : _tileCharacters[_direction];
        }
    }

    public class ArrowTile : Tile
    {
        public ArrowTile(TileDirection direction, Point position, bool isActive, TileMatrix tileMatrix) : base(direction, position, isActive, tileMatrix, "qwer", "QWER")
        {
            type = TileType.Arrow;
        }

        public override void tick()
        {
            // Логіка стрілочки
            // Паттерн проектування такий, що кожна стрілочка
            // Може змінювати лише свій стан
            bool[] neighboursSignals = getInputSignals();
            isActive = neighboursSignals.Any(x => x);
        }

        public override char getCharacter()
        {
            return isActive ? _tileCharactersIfActive[_direction] : _tileCharacters[_direction];
        }
    }

    public class DoubleArrowTile : Tile
    {
        public DoubleArrowTile(TileDirection direction, Point position, bool isActive, TileMatrix tileMatrix) : base(direction, position, isActive, tileMatrix, "qwer", "QWER")
        {
            type = TileType.Arrow;
        }

        public override void tick()
        {
            // Логіка стрілочки
            // Паттерн проектування такий, що кожна стрілочка
            // Може змінювати лише свій стан
            bool[] neighboursSignals = getInputSignals();
            isActive = neighboursSignals.Any(x => x);
        }

        public override char getCharacter()
        {
            return isActive ? _tileCharactersIfActive[_direction] : _tileCharacters[_direction];
        }
    }

    public class TakeTile : Tile
    {
        private Point inPoint;
        public TakeTile(TileDirection direction, Point position, bool isActive, TileMatrix tileMatrix) : base(direction, position, isActive, tileMatrix, "вгде", "ВГДЕ")
        {
            type = TileType.Take;
            inPoint = new Point(position.X - _currentDirection.X, position.Y - _currentDirection.Y);
        }

        public override void tick()
        {
            // Логіка стрілки взяття
            // Активний, якщо вхідний сигнал є істиним. Інакше - неактивний.
            isActive = tileMatrix.getTileAt(inPoint).isActive;
        }

        public override char getCharacter()
        {
            return isActive ? _tileCharactersIfActive[_direction] : _tileCharacters[_direction];
        }
    }

    public class OrTile : Tile
    {
        public OrTile(TileDirection direction, Point position, bool isActive, TileMatrix tileMatrix) : base(direction, position, isActive, tileMatrix, "tuxy", "TUXY")
        {
            type = TileType.Or;
        }

        public override void tick()
        {
            // Якщо будь-який із входів true - вихід true; інакше - false
            // По суті, це та ж сама стрілочка
            bool[] neighboursSignals = getInputSignals();
            isActive = neighboursSignals.Any(x => x);
        }

        public override char getCharacter()
        {
            return isActive ? _tileCharactersIfActive[_direction] : _tileCharacters[_direction];
        }
    }

    public class GeneratorTile : Tile
    {
        public GeneratorTile(TileDirection direction, Point position, TileMatrix tileMatrix) : base(direction, position, true, tileMatrix, "", "αβγδ")
        {
            type = TileType.Generator;
        }

        public override void tick()
        {
            // Генератор завжди активний, а тому реалізації тіку не має
        }

        public override char getCharacter()
        {
            return _tileCharactersIfActive[_direction];
        }
    }

    public class NoTile : Tile
    {
        public NoTile(TileDirection direction, Point position, TileMatrix tileMatrix, bool isActive) : base(direction, position, isActive, tileMatrix, "bfgh", "BFGH")
        {
            type = TileType.No;
        }

        public override void tick()
        {
            // Якщо хоча-б один сусід є true, то НІ стає false
            // Інакше - true
            bool[] neighboursSignals = getInputSignals();
            isActive = !neighboursSignals.Any(x => x);

            // На пам'ять
            // LogManager.addNote("WTF 'NO' TICK IS WORKING BUT IT DOESN'T UPDATE FUCKING BOOL IS ACTIVE");
        }

        public override char getCharacter()
        {
            char outChar = isActive ? _tileCharactersIfActive[_direction] : _tileCharacters[_direction];
            return isActive ? _tileCharactersIfActive[_direction] : _tileCharacters[_direction];
        }
    }

    public class AndTile : Tile
    {
        public AndTile(TileDirection direction, Point position, bool isActive, TileMatrix tileMatrix) : base(direction, position, isActive, tileMatrix, "poji", "POJI")
        {
            type = TileType.And;
        }

        public override void tick()
        {
            // Активуємо тільки якщо більше двух сигналів
            bool[] neighboursSignals = getInputSignals();
            int trueSignalsCount = neighboursSignals.Count(x => x);
            isActive = trueSignalsCount >= 2;
        }

        public override char getCharacter()
        {
            return isActive ? _tileCharactersIfActive[_direction] : _tileCharacters[_direction];
        }
    }

    public class EqualsTile : Tile
    {
        public EqualsTile(TileDirection direction, Point position, bool isActive, TileMatrix tileMatrix) : base(direction, position, isActive, tileMatrix, "kzlv", "KZLV")
        {
            type = TileType.Equals;
        }

        public override void tick()
        {
            int trueSignalsCount = 0;
            int totalSignals = 0;

            getInputSignals(ref trueSignalsCount, ref totalSignals);
            LogManager.addNote($"Result getInputSignals for equals: trueSignalsCount={trueSignalsCount} totalSignals={totalSignals}");

            // Активуємо тільки якщо 2 або більше сигнали та всі вони true
            isActive = totalSignals >= 2 && (trueSignalsCount == 0 || trueSignalsCount == totalSignals);
        }

        public override char getCharacter()
        {
            return isActive ? _tileCharactersIfActive[_direction] : _tileCharacters[_direction];
        }
    }

    public class XorTile : Tile
    {
        public XorTile(TileDirection direction, Point position, bool isActive, TileMatrix tileMatrix) : base(direction, position, isActive, tileMatrix, "mnаб", "MNАБ")
        {
            type = TileType.Xor;
        }

        public override void tick()
        {
            int trueSignalsCount = 0;
            int totalSignals = 0;

            getInputSignals(ref trueSignalsCount, ref totalSignals);
            LogManager.addNote($"Result getInputSignals for xor: trueSignalsCount={trueSignalsCount} totalSignals={totalSignals}");

            // XOR - це інвертоване equals. Тому саме так
            bool equalsCondition = totalSignals >= 2 && trueSignalsCount == totalSignals;
            isActive = totalSignals >= 2 && !equalsCondition;
        }

        public override char getCharacter()
        {
            return isActive ? _tileCharactersIfActive[_direction] : _tileCharacters[_direction];
        }
    }
}