using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace DungeonGeneration
{
    public class LayoutGenerator : MonoBehaviour 
    {
        /// <summary>
        /// X-position of start room in layout
        /// </summary>
        private const int StartRoomX = 4;

        /// <summary>
        /// Y-position of start room in layout
        /// </summary>
        private const int StartRoomY = 4;
        
        /// <summary>
        /// Distance between centres of rooms on X-coordinate
        /// </summary>
        private const int SceneRoomDistanceX = 15;

        /// <summary>
        /// Distance between centres of rooms on Y-coordinate
        /// </summary>
        private const int SceneRoomDistanceY = -9;
        
        /// <summary>
        /// Matrix that represents layout of the rooms
        ///
        /// 1 - Start
        /// 2 - EnemyEasy
        /// 3 - EnemyMedium
        /// 4 - EnemyHard
        /// 5 - Treasure
        /// 6 - Boss
        /// </summary>
        [SerializeField] private int[,] layout = new int[9, 9]
        {
            { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 1, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        };
        
        /// <summary>
        /// The current level of the dungeon
        /// </summary>
        private int level = 1;
        
        /// <summary>
        /// Number of rooms to generate
        /// </summary>
        private int numberOfRoomsToGenerate = 0;
        
        /// <summary>
        /// List that stores tuple of indexes of generated rooms in the layout matrix
        /// </summary>
        private List<(int, int)> generatedRooms = new List<(int, int)>();
        
        /// <summary>
        /// Number of end rooms to generate
        /// </summary>
        [SerializeField] private int minNumberOfEndRoomsToGenerate = 3;
        
        /// <summary>
        /// List that stores generated end rooms
        /// </summary>
        private List<(int, int)> generatedEndRooms = new List<(int, int)>();
        
        /// <summary>
        /// The boos room
        /// </summary>
        private (int, int) bossRoomPosition;
        
        /**
         * \brief Generating dungeon layout based on matrix 
         */
        public List<Room> Generate(int dungeonLevel)
        {
            level = dungeonLevel;
            numberOfRoomsToGenerate = Random.Range(0, 2) + 5 + level * 2;

            List<Room> roomsList = new List<Room>();
            while (true)
            {
                Reset();
                FillLayout((StartRoomX, StartRoomY));
                
                Debug.Log(generatedEndRooms.Count);
                if (generatedRooms.Count != numberOfRoomsToGenerate ||
                    generatedEndRooms.Count < minNumberOfEndRoomsToGenerate)
                {
                    continue;
                }
                
                FindEndRooms(); 
                //FindBossRoom();
                //FindTreasureRoom();
                    foreach ((int, int) position in generatedRooms)
                    { 
                        int x = (position.Item1 - StartRoomX) * SceneRoomDistanceX;
                        int y = (position.Item2 - StartRoomY) * SceneRoomDistanceY;

                        RoomType type = RoomType.Start;
                        int value = layout[position.Item1, position.Item2];
                        switch (value)
                        {
                            case 1:
                                type = RoomType.Start;
                                break;
                            case 2:
                                type = RoomType.EnemyEasy;
                                break;
                            case 3:
                                type = RoomType.EnemyMedium;
                                break;
                            case 4:
                                type = RoomType.EnemyHard;
                                break;
                            case 5:
                                type = RoomType.Treasure;
                                break;
                            case 6:
                                type = RoomType.Boss;
                                break;
                        }

                        List<Side> neighboringSides = FindNeighboringSides(position);
                        
                        Room room = new Room(x, y, type, neighboringSides);
                        roomsList.Add(room);
                    }
                    
                    //debugging layout
                    /*string matrix = "";
                    for (int i = 0; i < 9; ++i)
                    {
                        for (int j = 0; j < 9; ++j)
                        {
                            matrix += layout[i, j] + " ";
                        }

                        matrix += "\n";
                    }
                    Debug.Log(matrix);
                    */
                    return roomsList;
                }
        }
        
        /**
         * \brief Setting all values to the default
         */
        private void Reset()
        {
            for (int i = 0; i < 9; ++i)
            {
                for (int j = 0; j < 9; ++j)
                {
                    layout[i, j] = 0;
                }
            }
            
            generatedRooms.Clear();
            generatedEndRooms.Clear();
            
            layout[StartRoomX, StartRoomY] = 1;
            generatedRooms.Add((StartRoomX, StartRoomY));
        }

        private void FillLayout((int, int) position)
        {
            List<Side> sidesWithNeighbour = new List<Side>();
            
            // Choosing random sides to generate room spawn points on
            while (sidesWithNeighbour.Count == 0)
            {
                if (Random.Range(0, 2) == 0)
                    sidesWithNeighbour.Add(Side.Top);
                
                if (Random.Range(0, 2) == 0)
                    sidesWithNeighbour.Add(Side.Right);
                
                if (Random.Range(0, 2) == 0)
                    sidesWithNeighbour.Add(Side.Bottom);
                
                if (Random.Range(0, 2) == 0)
                    sidesWithNeighbour.Add(Side.Left);
            }

            foreach (Side side in sidesWithNeighbour)
            {
                var neighbourPosition = FindNeighbourPosition(position, side);
                // Checking if the cell is out of range
                if (neighbourPosition == (-1, -1))
                    continue;
                    
                // Checking if the cell is already occupied
                if (layout[neighbourPosition.Item1, neighbourPosition.Item2] != 0)
                    continue;
                
                // Checking if the cell already has more than one neighbour
                if (!HasOnlyOneNeighbour(neighbourPosition))
                    continue;
                
                // Checking if we already have enough rooms
                if (numberOfRoomsToGenerate == generatedRooms.Count)
                    return;
                
                // Random chance to give up
                if (Random.Range(0, 2) == 0)
                    continue;
                
                generatedRooms.Add(neighbourPosition);
                int roomDifficulty = FindRoomDifficulty();
                layout[neighbourPosition.Item1, neighbourPosition.Item2] = roomDifficulty;
                FillLayout(neighbourPosition);
            }
        }
        
        /**
         * \brief Calculating neighbour position for the cell in the layout matrix based on side you provide
         * \param position Indexes in the layout matrix of the cell you want to find neighbour position for
         * \param side Side of the neighbour which position you want to calculate
         * \return Tuple of the indexes of the neighbour cell
         */
        private (int, int) FindNeighbourPosition((int, int) position, Side side)
        {
            int x = position.Item1;
            int y = position.Item2;
            (int, int) neighbourPosition = (x, y);
            switch (side)
            {
                case Side.Top:
                    neighbourPosition = (x, y - 1);
                    break;
                case Side.Right:
                    neighbourPosition = (x + 1, y);
                    break;
                case Side.Bottom:
                    neighbourPosition = (x, y + 1);
                    break;
                case Side.Left:
                    neighbourPosition = (x - 1, y);
                    break;
            }

            if (neighbourPosition.Item1 < 0 || neighbourPosition.Item1 > 8 || neighbourPosition.Item2 < 0 ||
                neighbourPosition.Item2 > 8)
                return (-1, -1);

            return neighbourPosition;
        }
        
        /**
         * \brief Counting neighbours of the cell with provided position in the layout matrix
         * \param currentPosition Tuple of indexes of the cell in the layout matrix
         * \return Number of neighbours
         */
        private List<Side> FindNeighboringSides((int, int) cell)
        {
            List<Side> sides = new List<Side>();
            
            int x = cell.Item1;
            int y = cell.Item2;
            
            // Check top neighbor
            if (y > 0 && layout[x, y - 1] != 0)
                sides.Add(Side.Top);

            // Check right neighbor
            if (x < 8 && layout[x + 1, y] != 0)
                sides.Add(Side.Right);

            // Check bottom neighbor
            if (y < 8 && layout[x, y + 1] != 0)
                sides.Add(Side.Bottom);

            // Check left neighbor
            if (x > 0 && layout[x - 1, y] != 0)
                sides.Add(Side.Left);

            return sides;
        }
        
        /**
         * \brief Checking if cell with provided position in the layout matrix has only one neighbour
         * \param currentPosition Tuple of indexes of the cell in the layout matrix
         * \return "True" if cell has only one neighbour, "False" otherwise
         */
        private bool HasOnlyOneNeighbour((int, int) currentPosition)
        {
            return FindNeighboringSides(currentPosition).Count == 1;
        }
        
        /**
         * \brief Finding end cells and adding them to the generatedEndCells list
         */
        private void FindEndRooms()
        {
            foreach ((int, int) cell in generatedRooms)
            {
                if (HasOnlyOneNeighbour(cell) && cell != (StartRoomX, StartRoomY))
                {
                    generatedEndRooms.Add(cell);
                }
            }
        }

        private int FindRoomDifficulty()
        {
            int difficulty = 2;

            switch (level)
            {
                case 1:
                    difficulty = Random.Range(2, 4); // Selecting easy and medium
                    break;
                case 2:
                    difficulty = Random.Range(2, 5); // Selecting easy, medium and hard
                    break;
                case 3:
                    difficulty = Random.Range(3, 5); // Selecting medium and hard
                    break;
            }
            
            return difficulty;
        }
        
        /**
         * \brief Selects random EndRoom from the list, that is not already in use, and marks it as the TreasureRoom  
         */
        private void FindTreasureRoom()
        {
            while (true)
            {
                int i = Random.Range(0, generatedEndRooms.Count);
                
                (int, int) position = generatedEndRooms[i];
                
                if (layout[position.Item1, position.Item2] != 6)
                {
                    layout[position.Item1, position.Item2] = 5;
                    break;
                }
            }
        }
        
        /**
         * \brief Finds farthest EndRoom from the starting position and then marks it as the BossRoom
         */
        private void FindBossRoom()
        {
            int maxDistance = 0;

            foreach (var position in generatedEndRooms)
            {
                int distanceToStartRoomX = position.Item1 - StartRoomX;
                int distanceToStartRoomY = position.Item2 - StartRoomY;

                int distance = distanceToStartRoomX * distanceToStartRoomX + distanceToStartRoomY * distanceToStartRoomY;
                
                if (maxDistance < distance)
                {
                    maxDistance = distance;
                    bossRoomPosition = position;
                }
            }
            
            layout[bossRoomPosition.Item1, bossRoomPosition.Item2] = 6;
        }
        
    }
}

