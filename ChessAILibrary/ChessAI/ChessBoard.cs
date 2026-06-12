using System;
namespace ChessAI;

public interface ChessBoard
{
	
}

public struct Coordinate
{
    public Coordinate(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
    public Coordinate(Coordinate other)
    {
        x = other.x;
        y = other.y;
    }
    public int x;
    public int y;

    public bool Equals(Coordinate other)
    {
        return x == other.x && y == other.y;
    }
}

public struct Move
{
    public Move(Coordinate from, Coordinate to)
    {
        this.from = from;
        this.to = to;
        promotion = 0;
    }
    public Move(Coordinate from, Coordinate to, int promotion)
    {
        this.from = from;
        this.to = to;
        this.promotion = promotion;
    }
    public Coordinate from;
    public Coordinate to;
    public int promotion;

    public bool Equals(Move other)
    {
        return from.Equals(other.from) && to.Equals(other.to);
    }
}
