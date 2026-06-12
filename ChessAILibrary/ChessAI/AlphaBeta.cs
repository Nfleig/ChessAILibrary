using System;
using System.Collections.Generic;
using System.Threading;

namespace ChessAI;

public class AlphaBeta : ChessEngine
{
    private int TTU;
    public float grainSize;

    public AlphaBeta(Func<SimpleChess, float> _fitnessAlgorithm, bool _isBlack) : base(_fitnessAlgorithm, _isBlack)
    {

    }

    /// <summary>
    /// Generate the list of possible board states resulting from every possible move
    /// </summary>
    /// <param name="board"></param>
    /// <param name="_isBlack"></param>
    /// <returns></returns>
    List<SimpleChess> generateMoves(SimpleChess board, bool _isBlack)
    {
        List<SimpleChess> boards = new List<SimpleChess>();
        List<SimpleChess.Move> moves = board.generateMoves(_isBlack ? -1 : 1);
        foreach (SimpleChess.Move move in moves)
        {
            SimpleChess newBoard = new SimpleChess(board);
            newBoard.movePiece(move);
            boards.Add(newBoard);
        }
        return boards;
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
        moves.Sort(CompareMoves);
    }

    protected int CompareMoves(SimpleChess x, SimpleChess y)
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

    protected float CalculateAlphaBeta(SimpleChess board, bool ourTurn, float alpha, float beta, int depth, Dictionary<string, MTDNode> transpositionTable)
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
            return CalculateFitness(board);
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
                List<SimpleChess> newMoves = generateMoves(board, !_isBlack);
                if (newMoves.Count == 0)
                {
                    if ((_isBlack && board.BCheck) || (!_isBlack && board.WCheck))
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
                        fitness = Math.Max(fitness, CalculateAlphaBeta(move, false, tempAlpha, beta, depth - 1, transpositionTable));
                        Interlocked.Increment(ref _nodeCount);
                        tempAlpha = Math.Max(tempAlpha, fitness);
                    }
                }
            }
            else
            {
                if (board.isDraw())
                {
                    return 0;
                }
                List<SimpleChess> newMoves = generateMoves(board, _isBlack);
                if (newMoves.Count == 0)
                {
                    if ((!_isBlack && board.WCheck) || (_isBlack && board.BCheck))
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
                        fitness = Math.Min(fitness, CalculateAlphaBeta(move, true, alpha, tempBeta, depth - 1, transpositionTable));
                        Interlocked.Increment(ref _nodeCount);
                        tempBeta = Math.Min(fitness, tempBeta);
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
}

