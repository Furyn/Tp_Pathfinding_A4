using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Channels;
using System.Security.Cryptography.X509Certificates;

namespace Pathfinding
{

    class Game
    {
        const int MAP_WIDTH = 90;
        const int MAP_HEIGHT = 50;
        const int MAX_WEIGHT = 9;

        //ATTENTION: le tableau est construit en mode [hauteur][largeur].
        int[][] map;

        Position playerStartPos;
        Position goal;
        List<Position> doors = new List<Position>();

        public void Init()
        {
            Random random = new Random();

            map = new int[MAP_HEIGHT][];

            for (int i = 0; i < MAP_HEIGHT; ++i)
            {
                map[i] = new int[MAP_WIDTH];
                for (int j = 0; j < MAP_WIDTH; ++j)
                {
                    // On met des valeurs aléatoires dans chaque case, sauf sur les murs

                    map[i][j] = ((j == MAP_WIDTH / 3) || (j == MAP_WIDTH * 2 / 3) || (i == MAP_HEIGHT / 2)) ? int.MaxValue : random.Next(MAX_WEIGHT) + 1;
                }
            }

            //Joueur dans la salle 1
            playerStartPos.X = random.Next(MAP_WIDTH / 3);
            playerStartPos.Y = random.Next(MAP_HEIGHT / 2);

            //objectif dans la salle 3
            goal.X = (MAP_WIDTH * 2 / 3) + 1 + random.Next(MAP_WIDTH / 3 - 1);
            goal.Y = 1 + MAP_HEIGHT / 2 + random.Next(MAP_HEIGHT / 2 - 1);

            //Les portes
            //1 porte par mur vertical, + 1 sur le mur horizontal
            doors.Add(new Position(MAP_WIDTH / 3, random.Next(MAP_HEIGHT / 2)));
            doors.Add(new Position(MAP_WIDTH * 2 / 3, random.Next(MAP_HEIGHT / 2)));
            //1+ pour éviter qu'elles se retrouvent sur le mur horizontal
            doors.Add(new Position(MAP_WIDTH / 3, 1 + MAP_HEIGHT / 2 + random.Next(MAP_HEIGHT / 2 - 1)));
            doors.Add(new Position(MAP_WIDTH * 2 / 3, 1 + MAP_HEIGHT / 2 + random.Next(MAP_HEIGHT / 2 - 1)));
            //la porte horizontale, attention aux murs verticaux
            Position lastDoor = new Position(random.Next(MAP_WIDTH), MAP_HEIGHT / 2);
            while (lastDoor.X % (MAP_WIDTH / 3) == 0)
            {
                lastDoor.X = random.Next(MAP_WIDTH);
            };
            doors.Add(lastDoor);

            foreach (Position position in doors)
                map[position.Y][position.X] = 1;
        }

        public void Reconstruct_path(Dictionary<Position, Position> cameForm, Position current, List<Position> path)
        {
            path.Clear();
            path.Add(current);
            while ( cameForm.ContainsKey(current))
            {
                current = cameForm[current];
                path.Add(current);
            }
        }

        public int GetDistanceEnd(Position pos)
        {
            return Math.Abs(goal.X - pos.X) + Math.Abs(goal.Y - pos.Y);
        }

        //Fonction de calcul du chemin. Elle doit retourner les éléments suivants:
        //path: liste des points à traverser pour aller du départ à l'arrivée
        //cost: coût du trajet
        //checkedTiles: liste de toutes les positions qui ont été testées pour trouver le chemin le plus court
        public bool GetShortestPath(List<Position> path, out int cost, HashSet<Position> checkedTiles)
        {
            HashSet<Position> temp_checkedTiles = new HashSet<Position>();
            temp_checkedTiles.Add(playerStartPos);

            Dictionary<Position, Position> cameFrom = new Dictionary<Position, Position>();
            Dictionary<Position, int> costScore = new Dictionary<Position, int>();
            for (int X = 0; X < MAP_WIDTH; X++)
            {
                for (int y = 0; y < MAP_HEIGHT; y++)
                {
                    costScore[new Position(X, y)] = int.MaxValue;
                }
            }
            costScore[playerStartPos] = 0;

            Dictionary<Position, int> distanceScore = new Dictionary<Position, int>();
            distanceScore[playerStartPos] = GetDistanceEnd(playerStartPos);

            while (temp_checkedTiles.Count > 0)
            {
                Position currentPos = new Position();
                int min_distanceScore = int.MaxValue;
                foreach (Position tile in temp_checkedTiles)
                {
                    if (distanceScore[tile] < min_distanceScore)
                    {
                        min_distanceScore = distanceScore[tile];
                        currentPos = tile;
                    }
                }

                if (currentPos.Equals(goal))
                {
                    Reconstruct_path(cameFrom,currentPos,path);
                    cost = 0;
                    foreach (var item in path)
                    {
                        cost += map[item.Y][item.X];
                    }
                    return true;
                }
                temp_checkedTiles.Remove(currentPos);
                List<Position> neighbors = getNeighbors(currentPos);

                foreach (Position neighbor in neighbors)
                {
                    int tentative_gScore = costScore[currentPos] + map[neighbor.Y][neighbor.X];
                    if (tentative_gScore < costScore[neighbor])
                    {
                        cameFrom[neighbor] = currentPos;
                        costScore[neighbor] = tentative_gScore;
                        distanceScore[neighbor] = tentative_gScore + GetDistanceEnd(neighbor);
                        if (!temp_checkedTiles.Contains(neighbor))
                        {
                            checkedTiles.Add(neighbor);
                            temp_checkedTiles.Add(neighbor);
                        }
                    }
                }
               
            }

            cost = 0;
            return false;
        }

        public List<Position> getNeighbors(Position position)
        {
            List<Position> allPos = new List<Position>();

            if (position.X + 1 < MAP_WIDTH && map[position.Y][position.X + 1] != int.MaxValue)
            {
                allPos.Add(new Position(position.X + 1, position.Y));
                if (position.Y + 1 < MAP_HEIGHT && map[position.Y + 1][position.X + 1] != int.MaxValue)
                {
                    allPos.Add(new Position(position.X + 1, position.Y + 1));
                }
                if (position.Y - 1 >= 0 && map[position.Y - 1][position.X + 1] != int.MaxValue)
                {
                    allPos.Add(new Position(position.X + 1, position.Y - 1));
                }
            }
            if (position.X - 1 >= 0 && map[position.Y][position.X - 1] != int.MaxValue)
            {
                allPos.Add(new Position(position.X - 1, position.Y));
            }

            if (position.Y + 1 < MAP_HEIGHT && map[position.Y + 1][position.X] != int.MaxValue)
            {
                allPos.Add(new Position(position.X, position.Y + 1));
                if (position.X + 1 < MAP_WIDTH && map[position.Y + 1][position.X + 1] != int.MaxValue)
                {
                    allPos.Add(new Position(position.X + 1, position.Y + 1));
                }
                if (position.X - 1 >= 0 && map[position.Y + 1][position.X - 1] != int.MaxValue)
                {
                    allPos.Add(new Position(position.X - 1, position.Y + 1));
                }
            }
            if (position.Y - 1 >= 0 && map[position.Y - 1][position.X] != int.MaxValue)
            {
                allPos.Add(new Position(position.X, position.Y - 1));
            }

            return allPos;
        }

        public void DisplayMap(List<Position> path, HashSet<Position> checkedTiles)
        {

            ConsoleColor defaultColor = ConsoleColor.White; // Console.ForegroundColor;
            Position position = new Position();

            for (int i = 0; i < MAP_HEIGHT; ++i)
            {
                for (int j = 0; j < MAP_WIDTH; ++j)
                {
                    if (i == playerStartPos.Y && j == playerStartPos.X)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("S");
                    }
                    else if (i == goal.Y && j == goal.X)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("A");
                    }
                    else
                    {
                        Console.ForegroundColor = defaultColor;
                        position.X = j;
                        position.Y = i;
                        if (path.Contains(position))
                            Console.ForegroundColor = ConsoleColor.Red;
                        else if (checkedTiles.Contains(position))
                            Console.ForegroundColor = ConsoleColor.Blue;
                        else if (Math.Abs(map[i][j]) == int.MaxValue)
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write(Math.Abs(map[i][j]) != int.MaxValue ? "" + Math.Abs(map[i][j]) : "#");
                    }
                }
                Console.WriteLine();
            }

            Console.ForegroundColor = defaultColor;
        }
    };

    class MainClass
    {


        public static void Main(string[] args)
        {
            List<Position> path = new List<Position>();
            HashSet<Position> checkedTiles = new HashSet<Position>();
            int cost;
            Game game = new Game();
            Console.WriteLine("Initialisation....");
            game.Init();
            Console.WriteLine("Calcul du trajet....");
            bool found = game.GetShortestPath(path, out cost, checkedTiles);
            if (found)
            {
                Console.WriteLine("Trajet trouvé! longueur: {0}, coût: {1}, et on a du tester {2} positions pour l'obtenir.", path.Count, cost, checkedTiles.Count);
            }
            else
            {
                Console.WriteLine("Aucun trajet trouvé.");
            }

            game.DisplayMap(path, checkedTiles);

            Console.ReadKey();
        }
    }
}
