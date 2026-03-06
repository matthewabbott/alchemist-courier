using System.Collections.Generic;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Puzzle;
using AlchemistCourier.Core.Reagents;

namespace AlchemistCourier.Game.Inventory;

public class HintOrchestrator
{
    private readonly PuzzleState _puzzleData;
    private readonly HintState _hintState;
    private readonly MetaGrid _grid;
    private readonly List<Reagent> _trayReagents;

    public HintOrchestrator(PuzzleState puzzleData, HintState hintState, MetaGrid grid, List<Reagent> trayReagents)
    {
        _puzzleData = puzzleData;
        _hintState = hintState;
        _grid = grid;
        _trayReagents = trayReagents;
    }

    public bool HasHintsAvailable
    {
        get
        {
            if (_hintState.HasHintsRemaining) return true;
            for (int i = 0; i < _trayReagents.Count; i++)
            {
                if (!_trayReagents[i].IsPlaced && _hintState.WasHinted(i))
                    return true;
            }
            return false;
        }
    }

    public string FormatHintCount()
    {
        return $"{_hintState.HintsUsed}/{_hintState.MaxHints}";
    }

    public HintResult? ExecuteHint()
    {
        var reagentIndex = HintEngine.PickNextHintTarget(_puzzleData, _hintState, _trayReagents);
        if (reagentIndex == null) return null;

        var solutionPlacement = HintEngine.GetSolutionPlacement(_puzzleData, reagentIndex.Value);
        if (solutionPlacement == null)
            return new HintResult(HintResultType.NoSolutionFound, reagentIndex.Value);

        bool isSnapBack = _hintState.WasHinted(reagentIndex.Value);

        if (!isSnapBack && !_hintState.HasHintsRemaining)
            return new HintResult(HintResultType.NoHintsRemaining, reagentIndex.Value);

        var result = HintEngine.ExecuteHint(_grid, _trayReagents, reagentIndex.Value, solutionPlacement);

        if (result.IsSuccess && !isSnapBack)
        {
            _hintState.HintsUsed++;
            _hintState.HintedReagentIndices.Add(reagentIndex.Value);
        }

        return result;
    }
}
