using UnityEngine;

public interface IGimic
{
    abstract void OnSeasonChanged(GlobalSeason oldSeason, GlobalSeason newSeason);
}
