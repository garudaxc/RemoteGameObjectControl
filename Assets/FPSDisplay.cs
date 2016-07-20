using UnityEngine;
using System.Collections;
//ref: http://wiki.unity3d.com/wiki/index.php?title=FramesPerSecond
public class FPSDisplay : MonoBehaviour
{
	
	private float lastTime_;
	private int frameCount_;
	private static float interval_ = 1.0f;
	private float fps_;
	GUIStyle style;
	Rect rect;

	void Start(){
		int w = Screen.width, h = Screen.height;
		rect = new Rect(0, 20, w, h *2 / 50);
		
		style = new GUIStyle();
		style.alignment = TextAnchor.UpperLeft;
		style.fontSize = h * 2 / 50;
		style.normal.textColor = Color.yellow;
	}
	void Update()
	{
		frameCount_++;
		if (Time.unscaledTime - lastTime_ > interval_) {
			fps_ = (float)frameCount_ / (Time.unscaledTime - lastTime_);
			lastTime_ = Time.unscaledTime;
			frameCount_ = 0;
		}
	}

	void OnGUI()
	{
		string text = string.Format("{0:f2}", fps_);
		GUI.Label(rect, text, style);
	}
}