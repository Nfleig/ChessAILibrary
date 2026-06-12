using System;
using System.Collections.Generic;
using System.Threading;

public class ChessAI
{
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


    public ChessAI(int simulatedTurns, int color, float grainSize, float PieceWeight, float CenterWeight, float DevelopmentWeight, float PressureWeight, float KingWeight, float PawnWeight)
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


    public int simulatedTurns;
    public int color;
    public float grainSize;
    int nodeCount;
    private Func<SimpleChess, float> fitnessAlgorithm;
    public void IterativeDeepening(SimpleChess board, CancellationToken token)
    {
        float firstGuess = fitnessAlgorithm(board);
        Dictionary<string, MTDNode> transpositionTable = new Dictionary<string, MTDNode>();
        for (int d = 1; d < simulatedTurns; d++)
        {
            if (token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();
            }
            firstGuess = MTD(board, firstGuess, d, transpositionTable);
        }
        board.fitness = firstGuess;
    }
    public void IterativeDeepening(AIState state, CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            token.ThrowIfCancellationRequested();
        }
        state.currentGuess = MTD(state.board, state.currentGuess, state.depth, state.transpositionTable);
        state.board.fitness = state.currentGuess;
        state.depth++;
    }
    public void IterativeDeepening(SimpleChess board)
    {
        float firstGuess = fitnessAlgorithm(board);
        Dictionary<string, MTDNode> transpositionTable = new Dictionary<string, MTDNode>();
        for (int d = 1; d < simulatedTurns; d++)
        {
            firstGuess = MTD(board, firstGuess, d, transpositionTable);
        }
        board.fitness = firstGuess;
    }
    int searches = 0;
    float MTD(SimpleChess board, float firstGuess, int depth, Dictionary<string, MTDNode> transpositionTable)
    {
        float fitness = firstGuess;
        float upperBound = float.PositiveInfinity;
        float lowerBound = float.NegativeInfinity;
        float beta;
        while (!(lowerBound >= upperBound) && System.Math.Abs(fitness) != float.PositiveInfinity)
        {
            if (fitness == lowerBound)
            {
                beta = fitness + grainSize;
            }
            else
            {
                beta = fitness;
            }
            fitness = AlphaBeta(board, false, beta - grainSize, beta, depth, transpositionTable);
            Interlocked.Increment(ref nodeCount);
            Interlocked.Increment(ref searches);
            if (fitness < beta)
            {
                upperBound = fitness;
            }
            else
            {
                lowerBound = fitness;
            }
        }
        return fitness;
    }
    int TTU;

    float AlphaBeta(SimpleChess board, bool ourTurn, float alpha, float beta, int depth, Dictionary<string, MTDNode> transpositionTable)
    {
        string boardString = board.toString();
        if (transpositionTable.ContainsKey(boardString))
        {
            Interlocked.Increment(ref TTU);
            MTDNode node = transpositionTable[boardString];
            if (node.hasLower && node.lowerBound >= beta)
            {

                return node.lowerBound;
            }
            if (node.hasUpper && node.upperBound <= alpha)
            {
                return node.upperBound;
            }

            if (node.hasLower)
            {
                alpha = Math.Max(node.lowerBound, alpha);
            }
            if (node.hasUpper)
            {
                beta = Math.Min(node.upperBound, beta);
            }
        }
        if (depth <= 0)
        {
            return fitnessAlgorithm(board);
        }
        else
        {
            float fitness;
            if (ourTurn)
            {
                if (board.isDraw())
                {
                    return 0;
                }
                List<SimpleChess> newMoves = generateMoves(board, color);
                if (newMoves.Count == 0)
                {
                    if ((color < 0 && board.BCheck) || (color > 0 && board.WCheck))
                    {
                        return -100000;
                    }
                    else
                    {
                        return 0;
                    }
                }
                assignMoveOrder(newMoves);
                fitness = float.NegativeInfinity;
                float tempAlpha = alpha;
                foreach (SimpleChess move in newMoves)
                {
                    if (fitness >= beta)
                    {
                        break;
                    }
                    else
                    {
                        fitness = System.Math.Max(fitness, AlphaBeta(move, false, tempAlpha, beta, depth - 1, transpositionTable));
                        Interlocked.Increment(ref nodeCount);
                        tempAlpha = System.Math.Max(tempAlpha, fitness);
                    }
                }
            }
            else
            {
                if (board.isDraw())
                {
                    return 0;
                }
                List<SimpleChess> newMoves = generateMoves(board, -color);
                if (newMoves.Count == 0)
                {
                    if ((color < 0 && board.WCheck) || (color > 0 && board.BCheck))
                    {
                        return 100000;
                    }
                    else
                    {
                        return 0;
                    }
                }
                assignMoveOrder(newMoves);
                fitness = float.PositiveInfinity;
                float tempBeta = beta;
                foreach (SimpleChess move in newMoves)
                {
                    if (fitness <= alpha)
                    {
                        break;
                    }
                    else
                    {
                        fitness = System.Math.Min(fitness, AlphaBeta(move, true, alpha, tempBeta, depth - 1, transpositionTable));
                        Interlocked.Increment(ref nodeCount);
                        tempBeta = System.Math.Min(fitness, tempBeta);
                    }
                }
            }
            MTDNode node = new MTDNode(board, boardString);
            if (fitness <= alpha)
            {
                node.upperBound = fitness;
                node.hasUpper = true;
            }
            if (fitness > alpha && fitness < beta)
            {
                node.lowerBound = fitness;
                node.upperBound = fitness;
                node.hasUpper = true;
                node.hasLower = true;
            }
            if (fitness >= beta)
            {
                node.lowerBound = fitness;
                node.hasLower = true;
            }
            if (transpositionTable.ContainsKey(boardString))
            {
                transpositionTable[boardString] = node;
            }
            else
            {
                transpositionTable.Add(boardString, node);
            }
            return fitness;
        }
    }
    public float PieceWeight;
    public float CenterWeight;
    public float DevelopmentWeight;
    public float PressureWeight;
    public float KingWeight;
    public float PawnWeight;
    public static int[] pieceValue = { 0, 1, 3, 4, 4, 7, 10 };

    public float PawnAdvancementWeight;

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

    public void assignMoveOrder(List<SimpleChess> moves)
    {
        foreach (SimpleChess move in moves)
        {
            int order = 0;
            if (move.WCheck || move.BCheck)
            {
                order += 2;
            }
            if (move.capture)
            {
                order++;
            }
            move.calcOrder = order;
        }
        moves.Sort(MoveOrder);
    }
    int MoveOrder(SimpleChess x, SimpleChess y)
    {

        if (x.calcOrder == y.calcOrder)
        {
            return 0;
        }
        if ((x.calcOrder > y.calcOrder))
        {
            return -1;
        }
        return 1;
    }
    public void setFitnessAlgorithm(Func<SimpleChess, float> algorithm)
    {
        this.fitnessAlgorithm = algorithm;
    }
}

