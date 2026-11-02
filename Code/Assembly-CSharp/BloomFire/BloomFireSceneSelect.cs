using UnityEngine;
using UnityEngine.SceneManagement;

namespace BloomFire;

public class BloomFireSceneSelect : MonoBehaviour
{
	public bool GUIHide;

	public bool GUIHide2;

	public bool GUIHide3;

	public void LoadFireDemo01()
	{
		SceneManager.LoadScene("BloomFire01");
	}

	public void LoadFireDemo02()
	{
		SceneManager.LoadScene("BloomFire02");
	}

	public void LoadFireDemo03()
	{
		SceneManager.LoadScene("BloomFire03");
	}

	public void LoadFireDemo04()
	{
		SceneManager.LoadScene("BloomFire04");
	}

	public void LoadFireDemo05()
	{
		SceneManager.LoadScene("BloomFire05");
	}

	public void LoadFireDemo06()
	{
		SceneManager.LoadScene("BloomFire06");
	}

	public void LoadFireDemo07()
	{
		SceneManager.LoadScene("BloomFire07");
	}

	public void LoadFireDemo08()
	{
		SceneManager.LoadScene("BloomFire08");
	}

	public void LoadFireDemo09()
	{
		SceneManager.LoadScene("BloomFire09");
	}

	public void LoadFireDemo10()
	{
		SceneManager.LoadScene("BloomFire10");
	}

	public void LoadFireDemo11()
	{
		SceneManager.LoadScene("BloomFire11");
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.J))
		{
			GUIHide = !GUIHide;
			if (GUIHide)
			{
				GameObject.Find("CanvasSceneSelect").GetComponent<Canvas>().enabled = false;
			}
			else
			{
				GameObject.Find("CanvasSceneSelect").GetComponent<Canvas>().enabled = true;
			}
		}
		if (Input.GetKeyDown(KeyCode.K))
		{
			GUIHide2 = !GUIHide2;
			if (GUIHide2)
			{
				GameObject.Find("Canvas").GetComponent<Canvas>().enabled = false;
			}
			else
			{
				GameObject.Find("Canvas").GetComponent<Canvas>().enabled = true;
			}
		}
		if (Input.GetKeyDown(KeyCode.L))
		{
			GUIHide3 = !GUIHide3;
			if (GUIHide3)
			{
				GameObject.Find("CanvasTips").GetComponent<Canvas>().enabled = false;
			}
			else
			{
				GameObject.Find("CanvasTips").GetComponent<Canvas>().enabled = true;
			}
		}
	}
}
