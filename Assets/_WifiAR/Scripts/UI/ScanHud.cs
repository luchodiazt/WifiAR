using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WifiAR.UI
{
    /// <summary>The one card the user reads while scanning: what is happening and what to do next.</summary>
    public sealed class ScanHud : MonoBehaviour
    {
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text subtitle;
        [SerializeField] Graphic accent;

        public void Show(string titleText, string subtitleText, Color accentColor)
        {
            // Setting TMP text rebuilds its mesh, so skip it when nothing changed.
            if (title.text != titleText) title.text = titleText;
            if (subtitle.text != subtitleText) subtitle.text = subtitleText;
            accent.color = accentColor;
        }
    }
}
