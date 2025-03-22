using System.Drawing.Drawing2D;
using System.IO.Pipes;
using System.Xml.Serialization;

namespace WireWorld3dot0
{
    public class TileMatrix
    {
        public int width { get; private set; }
        public int height { get; private set; }
        private Tile[][] _matrix;

        public TileMatrix(int width, int height)
        {
            this.width = width;
            this.height = height;
            _matrix = new Tile[height][];

            generateEmptyMatrix();
        }
        
        // Конструктор копій
        public TileMatrix(TileMatrix other)
        {
            this.width = other.width;
            this.height = other.height;
            this._matrix = new Tile[height][];
            Tile currentTile;

            for (int y = 0; y < height; y++)
            {
                _matrix[y] = new Tile[width];
                for (int x = 0; x < width; x++)
                {
                    currentTile = other._matrix[y][x];
                    _matrix[y][x] = getInstanceOfTile(currentTile.type, currentTile.direction, currentTile.isActive, x, y, other);
                }
            }
        }

        public void CopyFrom(TileMatrix other)
        {
            width = other.width;
            height = other.height;
            Tile currentTile;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    currentTile = other._matrix[y][x];
                    _matrix[y][x] = getInstanceOfTile(currentTile.type, currentTile.direction, currentTile.isActive, x, y, other);
                }
            }
        }

        private void generateEmptyMatrix()
        {
            for (int y = 0; y < height; y++)
            {
                _matrix[y] = new Tile[width];
                for (int x = 0; x < width; x++)
                {
                    _matrix[y][x] = new EmptyTile(new Point(x, y), this);
                }
            }
        }

        public void clear()
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    setTileAtPoint(TileType.Empty, x, y);
                }
            }
        }

        public void tick()
        {
            LogManager.addNote("Матриця обробляє свой tick");
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if(getTypeOfTile(x, y) != TileType.Empty && getTypeOfTile(x, y) != TileType.Generator)
                        _matrix[y][x].tick();
                }
            }
        }

        private Tile getInstanceOfTile(TileType tileType, TileDirection tileDirection, bool isActive, Point position, TileMatrix matrix)
        {
            switch (tileType)
            {
                case TileType.Arrow:
                    return new ArrowTile(tileDirection, position, isActive, matrix);
                case TileType.Hold:
                    return new HoldTile(tileDirection, position, isActive, matrix);
                case TileType.DoubleArrow:
                    return new DoubleArrowTile(tileDirection, position, isActive, matrix);
                case TileType.Take:
                    return new TakeTile(tileDirection, position, isActive, matrix);
                case TileType.Generator:
                    return new GeneratorTile(tileDirection, position, matrix);
                case TileType.No:
                    return new NoTile(tileDirection, position, matrix, isActive);
                case TileType.Or:
                    return new OrTile(tileDirection, position, isActive, matrix);
                case TileType.And:
                    return new AndTile(tileDirection, position, isActive, matrix);
                case TileType.Equals:
                    return new EqualsTile(tileDirection, position, isActive, matrix);
                case TileType.Xor:
                    return new XorTile(tileDirection, position, isActive, matrix);
                case TileType.Empty:
                    return new EmptyTile(position, matrix);
                case TileType.Undefined:
                default:
                    return new UndefinedTile();
            }
        }

        private Tile getInstanceOfTile(TileType tileType, TileDirection tileDirection, bool isActive, int x, int y, TileMatrix matrix)
        {
            return getInstanceOfTile(tileType, tileDirection, isActive, new Point(x, y), matrix);
        }

        public bool isValidTile(int x, int y)
        {
            if (x < 0 || y < 0 || x > width - 1 || y > height - 1) return false;
            return true;
        }

        public bool isValidTile(Point position)
        {
            if (position.X < 0 || position.Y < 0 || position.X > width - 1 || position.Y > height - 1) return false;
            return true;
        }

        public char getCharOfTile(int x, int y)
        {
            if(!isValidTile(x, y)) return 'ё';
            return _matrix[y][x].getCharacter();
        }

        public TileType getTypeOfTile(int x, int y)
        {
            if (!isValidTile(x, y)) return TileType.Undefined;
            return _matrix[y][x].type;
        }

        public TileType getTypeOfTile(Point position)
        {
            if (!isValidTile(position.X, position.Y)) return TileType.Undefined;
            return _matrix[position.Y][position.X].type;
        }

        public Tile getTileAt(int x, int y)
        {
            if (!isValidTile(x, y)) return new UndefinedTile();
            return _matrix[y][x];
        }

        public Tile getTileAt(Point position)
        {
            if (!isValidTile(position.X, position.Y)) return new UndefinedTile();
            return _matrix[position.Y][position.X];
        }

        public void setTileAtPoint(TileType tileType, int x, int y, TileDirection tileDirection = TileDirection.NoDirection, bool isActive = false)
        {
            LogManager.addNote($"Setting tile at point x={x} y={y}");
            if (!isValidTile(x, y)) return;
            _matrix[y][x] = getInstanceOfTile(tileType, tileDirection, isActive, x, y, this);
        }

        public override string ToString()
        {
            string outString = string.Empty;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    outString += _matrix[y][x].isActive ? 1 : 0;
                }
                outString += "\n";
            }
            return outString;
        }
    }
}
