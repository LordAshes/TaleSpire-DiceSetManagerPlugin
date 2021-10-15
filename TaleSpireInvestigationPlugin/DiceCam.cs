using BepInEx;
using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace LordAshes
{
    public partial class DiceSetManagerPlugin : BaseUnityPlugin
    {
        private GameObject dolly = null;
        private Camera camera = null;

        public void DiceCamShow(bool display)
        {
            camera.enabled = display;
        }

        public void DiceCamSetup(float percentWidthPos, float percentHeightPos, float percentWidthSize, float percentHeightSize)
        {
            camera.rect = new Rect(percentWidthPos / 100f, percentHeightPos / 100f, percentWidthSize / 100f, percentHeightSize / 100f);
        }

        public void DiceCamMoveTo(Vector3 pos)
        {
            dolly.transform.position = new Vector3(pos.x,pos.y,pos.z);
        }

        public void DiceCamRotateTo(Vector3 rot)
        {
            camera.transform.rotation = Quaternion.Euler(rot.x, rot.y, rot.z);
        }

        public void DiceCamTrackDiceSet(string dicesetName)
        {
            Debug.Log("Dice Set Manager Plugin: Tracking Diceset "+dicesetName);
            DiceCamShow(true);
            Instance.rollId = DiceSets[dicesetName].RollId;
            StartCoroutine(Tracking(0.050f));
        }

        private IEnumerator Tracking(float waitTime)
        {
            DiceManager dm = DiceManager.Instance;
            DiceSet ds = FindByRollId(Instance.rollId).Value;
            Vector3 offset = dolly.transform.position - ds.Dice[0].gameObject.transform.position;
            while (true)
            {
                bool diceStopped = true;
                foreach (Die die in ds.Dice)
                {
                    if (die.IsRolling) { diceStopped = false; break; }
                }
                if (diceStopped)
                { 
                    Debug.Log("Dice Set Manager Plugin: Stopped Tracking Diceset");
                    waitTime = 3f;
                    yield return new WaitForSeconds(waitTime);
                    DiceCamShow(false);
                    break; 
                }
                dolly.transform.position = ds.Dice[0].gameObject.transform.position + offset;
                yield return new WaitForSeconds(waitTime);
            }
        }
    }
}
