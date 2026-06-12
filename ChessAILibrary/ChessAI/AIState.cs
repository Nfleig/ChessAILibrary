using System;
using System.Collections.Generic;

public class AIState
{
    public SimpleChess board;
    public Dictionary<string, MTDNode> transpositionTable;
    public float currentGuess;
    public int depth;

    public AIState(SimpleChess board, float currentGuess)
    {
        this.board = board;
        this.transpositionTable = new Dictionary<string, MTDNode>();
        this.currentGuess = currentGuess;
        this.depth = 1;
    }

}

