using System;
using System.Collections.Generic;
using System.Threading;

public abstract class ChessEngine
{
    protected Func<SimpleChess, float> _fitnessAlgorithm;
    protected int _nodeCount;
    protected bool _isBlack;

    public int NodeCount => _nodeCount;

    public bool IsBlack => _isBlack;

    public void setFitnessAlgorithm(Func<SimpleChess, float> _fitnessAlgorithm)
    {
        this._fitnessAlgorithm = _fitnessAlgorithm;
    }

    public float CalculateFitness(SimpleChess board)
    {
        return _fitnessAlgorithm(board);
    }

    public ChessEngine(Func<SimpleChess, float> _fitnessAlgorithm, bool _isBlack)
    {
        this._fitnessAlgorithm = _fitnessAlgorithm;
        this._isBlack = _isBlack;
    }
}

public struct MTDNode
{
    public MTDNode(SimpleChess board, string boardString)
    {
        this.board = board;
        this.boardString = boardString;
        this.upperBound = 0;
        this.lowerBound = 0;
        this.hasUpper = false;
        this.hasLower = false;
    }
    public string boardString;
    public SimpleChess board;
    public bool hasUpper;
    public bool hasLower;
    public float upperBound;
    public float lowerBound;
}

public class ChessAIEngine
{
    // Public variables

    public int simulatedTurns;
    public int color;
    public float grainSize;

    // Fitness Algorithm

    public float PieceWeight;
    public float CenterWeight;
    public float DevelopmentWeight;
    public float PressureWeight;
    public float KingWeight;
    public float PawnWeight;
    public static int[] pieceValue = { 0, 1, 3, 4, 4, 7, 10 };

    public float PawnAdvancementWeight;

    // Private variables

    private int nodeCount;
    private Func<SimpleChess, float> fitnessAlgorithm;
    private int searches = 0;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="simulatedTurns">The number of simulated turns for MTD</param>
    /// <param name="color">The AI's color</param>
    /// <param name="grainSize">The grain size for Alpha-Beta pruning</param>
    /// <param name="PieceWeight">The piece weight for the fitness algorithm</param>
    /// <param name="CenterWeight">The center weight for the fitness algorithm</param>
    /// <param name="DevelopmentWeight">The development weight for the fitness algorithm</param>
    /// <param name="PressureWeight">The pressure weight for the fitness algorithm</param>
    /// <param name="KingWeight">The king weight for the fitness algorithm</param>
    /// <param name="PawnWeight">The pawn weight for the fitness algorithm</param>
    public ChessAIEngine(int simulatedTurns, int color, float grainSize, float PieceWeight, float CenterWeight, float DevelopmentWeight, float PressureWeight, float KingWeight, float PawnWeight)
    {
        this.simulatedTurns = simulatedTurns;
        this.color = color;
        this.grainSize = grainSize;
        this.PieceWeight = PieceWeight;
        this.CenterWeight = CenterWeight;
        this.DevelopmentWeight = DevelopmentWeight;
        this.PressureWeight = PressureWeight;
        this.KingWeight = KingWeight;
        this.PawnWeight = PawnWeight;
        this.fitnessAlgorithm = CalculateFitness;
    }

    //Fitness algorithm based on chess board evaluation guide at https://chessfox.com/example-of-the-complete-evaluation-process-of-chess-a-chess-position/

    public float CalculateFitness(SimpleChess board)
    {
        float fitness = 0;
        List<SimpleChess.Coordinate> newBoard = new List<SimpleChess.Coordinate>(board.getPieces(1));
        newBoard.AddRange(board.getPieces(-1));
        foreach (SimpleChess.Coordinate location in newBoard)
        {
            int piece = board.testSpace(location);
            if (System.Math.Abs(piece) > 6 && piece != 0)
            {
                continue;
            }
            if (System.Math.Abs(piece) == 1)
            {
                int pawnScore = 0;
                if (board.isThreatened(location, color))
                {
                    pawnScore += 2;
                }
                if (board.testSpace(location.x + 1, location.y + piece) * piece < 0 || board.testSpace(location.x - 1, location.y + piece) * piece < 0)
                {
                    pawnScore += 3;
                }
                fitness += pawnScore * (PawnWeight * color * piece);
            }
            else if (System.Math.Abs(piece) == 6)
            {
                int protectionScore = 0;
                if (location.x < 3)
                {
                    protectionScore = 3 - location.x;
                }
                else if (location.x > 4)
                {
                    protectionScore = location.x - 4;
                }
                else
                {
                    protectionScore = 0;
                }
                if (location.y < 3)
                {
                    protectionScore = 3 - location.y;
                }
                else if (location.y > 4)
                {
                    protectionScore = location.y - 4;
                }
                else
                {
                    protectionScore = 0;
                }
                fitness += protectionScore * (1 / 6) * KingWeight;
            }
            else
            {
                List<SimpleChess.Move> moves = board.generateMoves(location);
                int moveCount = moves.Count;
                foreach (SimpleChess.Move move in moves)
                {
                    int hitPiece = board.testSpace(move.to);
                    int centerIndex = System.Math.Min(System.Math.Abs(4 - move.to.x), System.Math.Abs(3 - move.to.x)) + System.Math.Min(System.Math.Abs(4 - move.to.y), System.Math.Abs(3 - move.to.y));
                    if (piece * color > 0)
                    {
                        fitness += pieceValue[System.Math.Abs(hitPiece)] * PressureWeight;
                        if (centerIndex <= 2)
                        {
                            fitness += (3 - centerIndex) * CenterWeight * (1 / 3);
                        }
                    }
                    else
                    {
                        fitness -= pieceValue[System.Math.Abs(hitPiece)] * PressureWeight;
                        if (centerIndex <= 2)
                        {
                            fitness -= (3 - centerIndex) * CenterWeight * (1 / 3);
                        }
                    }
                }
                if (piece * color > 0)
                {
                    fitness += pieceValue[System.Math.Abs(piece)] * PieceWeight / 47f;
                    fitness += moveCount * DevelopmentWeight;

                }
                else
                {
                    fitness -= pieceValue[System.Math.Abs(piece)] * PieceWeight / 47;
                    fitness -= moveCount * DevelopmentWeight;
                }
            }

        }
        return fitness;
    }
    List<SimpleChess> generateMoves(SimpleChess board, int color)
    {
        List<SimpleChess> boards = new List<SimpleChess>();
        List<SimpleChess.Move> moves = board.generateMoves(color);
        foreach (SimpleChess.Move move in moves)
        {
            SimpleChess newBoard = new SimpleChess(board);
            newBoard.movePiece(move);
            boards.Add(newBoard);
        }
        return boards;
    }
    public int getNodeCount()
    {
        return nodeCount;
    }
    public int getTTU()
    {
        return TTU;
    }
    public int getSearches()
    {
        return searches;
    }
    public void reset()
    {
        nodeCount = 0;
        TTU = 0;
        searches = 0;
    }
    public void setWeights(float PieceWeight, float CenterWeight, float DevelopmentWeight, float PressureWeight, float KingWeight, float PawnWeight)
    {
        this.PieceWeight = PieceWeight;
        this.CenterWeight = CenterWeight;
        this.DevelopmentWeight = DevelopmentWeight;
        this.PressureWeight = PressureWeight;
        this.KingWeight = KingWeight;
        this.PawnWeight = PawnWeight;
    }

    public void setFitnessAlgorithm(Func<SimpleChess, float> algorithm)
    {
        this.fitnessAlgorithm = algorithm;
    }
}

