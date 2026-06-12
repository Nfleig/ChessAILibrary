using System;
using System.Collections.Generic;
using System.Threading;

namespace ChessAI;

public class MTD : AlphaBeta
{
    private int _simulatedTurns;
    private int _searches;

    public MTD(Func<SimpleChess, float> _fitnessAlgorithm, bool _isBlack, int _simulatedTurns) : base(_fitnessAlgorithm, _isBlack)
    {
        this._simulatedTurns = _simulatedTurns;
        _searches = 0;
    }

    /// <summary>
    /// Begins the iterative deepening algorithm for MTD
    /// </summary>
    /// <param name="board">The current board state</param>
    /// <param name="token">The cancellation token</param>
    public void IterativeDeepening(SimpleChess board, CancellationToken token)
    {
        // Get the current fitness
        float firstGuess = CalculateFitness(board);

        // Initialize the transposition table
        Dictionary<string, MTDNode> transpositionTable = new Dictionary<string, MTDNode>();
        for (int d = 1; d < _simulatedTurns; d++)
        {
            if (token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();
            }
            firstGuess = CalculateMTD(board, firstGuess, d, transpositionTable);
        }
        board.fitness = firstGuess;
    }

    /// <summary>
    /// Runs the iterative deepening algorithm for MTD
    /// </summary>
    /// <param name="state"></param>
    /// <param name="token"></param>
    public void IterativeDeepening(AIState state, CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            token.ThrowIfCancellationRequested();
        }
        state.currentGuess = CalculateMTD(state.board, state.currentGuess, state.depth, state.transpositionTable);
        state.board.fitness = state.currentGuess;
        state.depth++;
    }

    /// <summary>
    /// Runs the iterative deepening algorithm for MTD
    /// </summary>
    /// <param name="board"></param>
    public void IterativeDeepening(SimpleChess board)
    {
        float firstGuess = CalculateFitness(board);
        Dictionary<string, MTDNode> transpositionTable = new Dictionary<string, MTDNode>();
        for (int d = 1; d < _simulatedTurns; d++)
        {
            firstGuess = CalculateMTD(board, firstGuess, d, transpositionTable);
        }
        board.fitness = firstGuess;
    }

    /// <summary>
    /// Runs a pass of the MTD algorithm
    /// </summary>
    /// <param name="board">The starting board state</param>
    /// <param name="firstGuess"></param>
    /// <param name="depth"></param>
    /// <param name="transpositionTable"></param>
    /// <returns></returns>
    float CalculateMTD(SimpleChess board, float firstGuess, int depth, Dictionary<string, MTDNode> transpositionTable)
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
            fitness = CalculateAlphaBeta(board, false, beta - grainSize, beta, depth, transpositionTable);
            Interlocked.Increment(ref _nodeCount);
            Interlocked.Increment(ref _searches);
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
}

