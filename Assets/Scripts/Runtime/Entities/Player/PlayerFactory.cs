﻿namespace Maze.Runtime.Entities
{
    public class PlayerFactory
    {
        public PlayerBuilder Create()
        {
            return new PlayerBuilder();
        }
    }
}
