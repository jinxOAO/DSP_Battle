using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSP_Battle
{
    public class EnemySorterSpace
    {
        public int starIndex; // 机甲所在星系
        public EEnemySearchMode searchMode;
        public int sortInterval; // 每多少帧重新寻敌

        public EnemySorterSpace(int starIndex, EEnemySearchMode searchMode)
        {
            this.starIndex = starIndex;
            this.searchMode = searchMode;
            sortInterval = 60;
            if (GameMain.localStar != null)
                this.starIndex = GameMain.localStar.index;
        }

        public void GameTick()
        {
            if (searchMode == EEnemySearchMode.None)
            {
                return;
            }
            else if (searchMode == EEnemySearchMode.NearMechaCurStar)
            {

            }
        }

        public void SearchAndSort()
        {

        }
    }

    public enum EEnemySearchMode
    {
        None,
        NearMechaCurStar,
    }
}
