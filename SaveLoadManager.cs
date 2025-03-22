using System;
using System.IO;
using System.Text;

namespace WireWorld3dot0
{
    // Клас для збереження та завантаження мап
    public static class SaveLoadManager
    {
        private const string FILE_EXTENSION = ".logmap";

        public static void SaveMap(string fileName, TileMatrix gameMatrix)
        {
            string filePath = fileName + FILE_EXTENSION;

            using (BinaryWriter writer = new BinaryWriter(File.Open(filePath, FileMode.Create)))
            {
                writer.Write((short)gameMatrix.width);
                writer.Write((short)gameMatrix.height);
                Tile currentTile;

                for (int y = 0; y < gameMatrix.height; y++)
                {
                    for (int x = 0; x < gameMatrix.width; x++)
                    {
                        currentTile = gameMatrix.getTileAt(x, y);
                        if (currentTile.type == TileType.Empty || currentTile.type == TileType.Undefined) continue;

                        writer.Write((short)currentTile.type);
                        writer.Write((short)x);
                        writer.Write((short)y);
                        writer.Write((short)currentTile.direction);
                        writer.Write(currentTile.isActive);
                    }
                }
            }
        }

        public static void LoadMap(string fileName, ref TileMatrix gameMatrix)
        {
            string filePath = fileName + FILE_EXTENSION;

            if (!File.Exists(filePath))
            {
                LogManager.addNote($"File {filePath} does not exist!");
                return;
            }

            using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
            {
                try
                {
                    short width = reader.ReadInt16();
                    short height = reader.ReadInt16();

                    if (gameMatrix.width != width || gameMatrix.height != height)
                    {
                        gameMatrix = new TileMatrix(width, height);
                    }
                    else
                    {
                        gameMatrix.clear();
                    }

                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        // Читаем тип тайла
                        TileType tileType = (TileType)reader.ReadInt16();

                        // Читаем позицию
                        short posX = reader.ReadInt16();
                        short posY = reader.ReadInt16();

                        // Читаем направление
                        TileDirection direction = (TileDirection)reader.ReadInt16();

                        // Читаем состояние активности
                        bool isActive = reader.ReadBoolean();

                        // Устанавливаем тайл в игровой матрице
                        gameMatrix.setTileAtPoint(tileType, posX, posY, direction, isActive);
                    }

                    LogManager.addNote($"Successfully loaded map from {filePath}");
                }
                catch (Exception ex)
                {
                    LogManager.addNote($"Error loading map: {ex.Message}");
                }
            }
        }
    }
}