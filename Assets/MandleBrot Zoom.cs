using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MandleBrotZoom : MonoBehaviour
{
    public PresetList2SO presetList2;
    public RawImage image;
    public Vector2 screenpos;
    public float screenscale;
    public Material MandleBrot;
    public Renderer ObjectRenderer;
    public float scint;
    public float shift;
    public float cycle = 20f;
    public float rot = 0f;

    private float pickoverlinear = -2.59026716545f;
    private float pickoverscale = 0.075f;
    public Slider pickoverslider;
    private float pickovermin = -9f;
    private float pickovermax = 8.5f;

    //public float juliaroot_x = 0.1f;
    //public float juliaroot_y = 0.7f;
    public Vector2 juliaroot = new Vector2(0.1f, 0.7f);

    public int FractalType = 0;
    public int isJuliaTarget = 0;
    public int keydowncount = 0;

    public Image backgroundimage;
    private float originalbackgroundalpha;
    public TextMeshProUGUI titletext;
    private Color titleColor;
    private Vector3 titlePosition;
    private bool startup = true;
    private int startup_stage = -1;
    private float startstage_time;

    private float title_fadein = 1f;
    private float title_pause = 1.5f;
    private float title_rise_fadeout = 1f;
    private float title_rise_distance = 30f;
    private float title_post_pause = 0f;
    private float background_fade_time = 1.5f;

    public GameObject ui;
    public GameObject fractalselectionui;
    public GameObject pickoversliderui;
    public GameObject statsui;
    public GameObject saveuinextarrow;
    public GameObject saveui;
    public TMP_InputField saveuiname;

    public Text statstext;

    private bool uiactive = false;

    private bool uifadein = false;
    private bool uifadeout = false;

    private bool fractalfadein = false;
    private bool fractalfadeout = false;
    private float fractalchange = 0f;
    private float fractalfadetime = 0.3f;
    private float uichange = 0f;
    private bool uifinished = false;

    private int ispickover = 1;

    private bool first = true;

    private float initialtouchdistance = 0f;

    private int uipanel = 0;

    private float uitime = 0.3f;
    private float uifadetime = 0.3f;
    private float uiselectsize = 1.1f;

    public RectTransform mandelbrot;
    public RectTransform julia;

    private Vector3 mandelbrotsize;
    private Vector3 juliasize;

    private Vector3 mandelbrotstartsize;
    private Vector3 juliastartsize;

    public CanvasGroup canvasGroup;

    private float touchstart = 0f;
    private bool touchmoving = false;
    private bool touchfading = false;
    private float longtouchtime = 0.5f;

    void PickRandomPreset()
    {
        if (presetList2 == null || presetList2.presets.Count == 0)
        {
            return;
        }

        int randomIndex = Random.Range(0, presetList2.presets.Count);
        Preset2SO randomPreset = presetList2.presets[randomIndex];

        FractalType = randomPreset.type;
        isJuliaTarget = FractalType;
        scint = randomPreset.scint;
        screenpos = randomPreset.screenpos;
        pickoverlinear = randomPreset.pickoverlinear;
        juliaroot = randomPreset.root;
    }

    // Start is called before the first frame update
    void Start()
    {
        startup = true;
        startstage_time = Time.time;
        //originalbackgroundcolor = backgroundimage.color;
        originalbackgroundalpha = backgroundimage.color.a;

        /* Dumb getter/setter syntax */
        titleColor = titletext.color;
        Color newColor = titleColor;
        newColor.a = 0f;
        titletext.color = newColor;

        titlePosition = titletext.rectTransform.localPosition;
        screenscale = .2f;

        screenpos = new Vector2(-25f, 0);
        PickRandomPreset();

        MandleBrot.SetVector("_Area" , new Vector4(screenpos.x , screenpos.y , screenscale , screenscale));

        //mandelbrotsize = mandelbrot.sizeDelta;
        //juliasize = julia.sizeDelta;
        mandelbrotsize = mandelbrot.localScale;
        juliasize = julia.localScale;

        pickoverslider.value = PickoverToLinear(pickoverlinear);

# if UNITY_EDITOR
        saveuinextarrow.SetActive(true);
# endif
    }

    public void FractalSelection()
    {
        pickoversliderui.SetActive(false);
        fractalselectionui.SetActive(true);
    }

    public void StatsSelection()
    {
        pickoversliderui.SetActive(false);
        statsui.SetActive(true);
        saveui.SetActive(false);
        saveuiname.text = "";
    }

    public void SaveSelection()
    {
        statsui.SetActive(false);
        saveui.SetActive(true);
    }

    public void SaveUiFinshedEditing()
    {
        if (presetList2 == null)
        {
            Debug.Log("PresetList2 is null");
            return;
        }

        if (saveuiname.text == "")
        {
            Debug.Log("No name entered");
            return;
        }
        for (int i = 0; i < presetList2.presets.Count; i++)
        {
            if (presetList2.presets[i].name == saveuiname.text)
            {
                Debug.Log("Preset with name " + saveuiname.text + " already exists");
                return;
            }
        }
# if UNITY_EDITOR
        Preset2SO preset = ScriptableObject.CreateInstance<Preset2SO>();
        preset.type = FractalType;
        preset.name = saveuiname.text;
        preset.scint = scint;
        preset.screenpos = screenpos;
        preset.pickoverlinear = pickoverlinear;
        preset.root = juliaroot;
        AssetDatabase.CreateAsset(preset, "Assets/Presets/" + saveuiname.text + ".asset");
        presetList2.presets.Add(preset);
        EditorUtility.SetDirty(presetList2);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
# endif
    }

    private void SetStatsText()
    {
        statstext.text = "Screen Position: " + screenpos.x + "," + screenpos.y + "\nScreen Scale: " + scint + "\nStalk Density: " + pickoverlinear;
    }

    public void PickoverSelection()
    {
        fractalselectionui.SetActive(false);
        pickoversliderui.SetActive(true);
        statsui.SetActive(false);
    }

    public void UiGetStartSize()
    {
        mandelbrotstartsize = mandelbrot.localScale;
        juliastartsize = julia.localScale;
    }

    public void UiReset()
    {
        mandelbrot.localScale = mandelbrotsize;
        julia.localScale = juliasize;
        UiGetStartSize();
    }

    public void UiFinish()
    {
        if (isJuliaTarget == 0) {
            mandelbrot.localScale = mandelbrotsize * uiselectsize;
            julia.localScale = juliasize;
        } else {
            mandelbrot.localScale = mandelbrotsize;
            julia.localScale = juliasize * uiselectsize;
        }
    }

    public void FractalFadeUpdate()
    {
        if (fractalfadeout) {
            float t = (Time.time - fractalchange) / fractalfadetime;
            MandleBrot.SetFloat("_FractalFade" , 1 - t);
            if (Time.time - fractalchange > fractalfadetime) {
                fractalfadeout = false;
                MandleBrot.SetFloat("_FractalFade" , 0);
                FractalType = isJuliaTarget;
                if (FractalType == 1 || FractalType == 2) {
                    juliaroot = new Vector2(0.1f, 0.7f);
                }
                fractalfadein = true;
                fractalchange = Time.time;
            }
        } else if (fractalfadein) {
            float t = (Time.time - fractalchange) / fractalfadetime;
            MandleBrot.SetFloat("_FractalFade" , t);
            if (Time.time - fractalchange > fractalfadetime) {
                fractalfadein = false;
                MandleBrot.SetFloat("_FractalFade" , 1);
            }
        }
    }

    public void UiUpdate()
    {
        if (uipanel == 0) {
            float t = (Time.time - uichange) / uitime;
            if (isJuliaTarget == 0) {
                mandelbrot.localScale = Vector3.Lerp(mandelbrotstartsize,
                            mandelbrotsize * uiselectsize, t);
                julia.localScale = Vector2.Lerp(juliastartsize, juliasize, t);
            } else {
                mandelbrot.localScale = Vector2.Lerp(mandelbrotstartsize, mandelbrotsize, t);
                julia.localScale = Vector2.Lerp(juliastartsize, juliasize * uiselectsize, t);
            }
            Debug.Log("t = " + t + " mandelbrot.localScale = " + mandelbrot.localScale + " julia.localScale = " + julia.localScale);
        }
    }

    public void MandleBrotToggled(bool value)
    {
        if (uifadein || uifadeout || fractalfadein || fractalfadeout) {
            return;
        }
        if (FractalType == 1)
        {
            isJuliaTarget = 0;
            //MandleBrot.SetInt("_IsJulia" , isJulia);
            uichange = Time.time;
            uifinished = false;
            UiGetStartSize();

            fractalchange = Time.time;
            fractalfadeout = true;
        }
    }

    private float LinearToPickover(float val)
    {
        return pickovermin + (pickovermax - pickovermin) * (val - pickoverslider.minValue) / (pickoverslider.maxValue - pickoverslider.minValue);
    }

    private float PickoverToLinear(float val)
    {
        return pickoverslider.minValue + (pickoverslider.maxValue - pickoverslider.minValue) * (val - pickovermin) / (pickovermax - pickovermin);
    }

    public void PickoverSlider(float val)
    {
        pickoverlinear = LinearToPickover(pickoverslider.value);
        pickoverscale = Mathf.Exp(pickoverlinear);
        //Debug.Log("pickoverlinear " + pickoverlinear + " pickoverscale " + pickoverscale);
        //MandleBrot.SetFloat("_PickoverScale" , pickoverscale);
    }

    public void JuliaToggled(bool value)
    {
        if (uifadein || uifadeout || fractalfadein || fractalfadeout) {
            return;
        }
        if (FractalType == 0)
        {
            isJuliaTarget = 1;
            //MandleBrot.SetInt("_IsJulia" , isJulia);
            uichange = Time.time;
            uifinished = false;
            UiGetStartSize();

            fractalchange = Time.time;
            fractalfadeout = true;
        }
    }

    public void CloseUi(bool value)
    {
        if (uifadein || uifadeout || fractalfadein || fractalfadeout) {
            return;
        }
        uifadeout = true;
        canvasGroup.alpha = 1;
        uiactive = false;
        uichange = Time.time;
        uifinished = false;
    }

    IEnumerator FadeBackground()
    {
        float t = 0f;
        float target = title_fadein + title_pause + title_rise_fadeout + title_post_pause + background_fade_time;
        Color newColor;
        while (t < target) {
            t += Time.deltaTime;
            float a = Mathf.Lerp(originalbackgroundalpha, 0f, t / target);
            newColor = backgroundimage.color;
            newColor.a = a;
            backgroundimage.color = newColor;
            yield return null;
        }
        ui.SetActive(false);
        fractalselectionui.SetActive(true);
        newColor = backgroundimage.color;
        newColor.a = originalbackgroundalpha;
        backgroundimage.color = newColor;
        startup = false;
    }

    void StartupUpdate()
    {
        if (startup_stage == -1) {
            startup_stage = 0;
            startstage_time = Time.time;
            StartCoroutine(FadeBackground());
        }

        /* Stage 0 : fade-in */
        if (startup_stage == 0) {
            float t = (Time.time - startstage_time) / title_fadein;
            if ((Time.time - startstage_time) > title_fadein) {
                t = 1f;
                startup_stage = 1;
                startstage_time = Time.time;
            }
            Color newColor = titletext.color;
            newColor.a = t;
            titletext.color = newColor;
            return;
        }

        if (startup_stage == 1) {
            if ((Time.time - startstage_time) > title_pause) {
                startup_stage = 2;
                startstage_time = Time.time;
            }
            return;
        }
        if (startup_stage == 2) {
            float t = (Time.time - startstage_time) / title_rise_fadeout;
            if ((Time.time - startstage_time) > title_rise_fadeout) {
                t = 1f;
                startup_stage = 3;
                startstage_time = Time.time;
            }
            Color newColor = titletext.color;
            newColor.a = (1f - t);
            titletext.color = newColor;

            Vector3 targetpos = titlePosition + new Vector3(0, title_rise_distance, 0);
            Vector3 newpos = Vector3.Lerp(titlePosition, targetpos, t);
            titletext.rectTransform.localPosition = newpos;

            return;
        }

        if (startup_stage == 3) {
            if ((Time.time - startstage_time) > title_post_pause) {
                startup_stage = 4;
                startstage_time = Time.time;
            }
            return;
        }

        /*
        if (startup_stage == 4) {
            if ((Time.time - startstage_time) > background_fade_time) {
                startup_stage = 5;
                startstage_time = Time.time;
                ui.SetActive(false);
            }
            float t = (Time.time - startstage_time) / background_fade_time;
            float a = Mathf.Lerp(originalbackgroundalpha, 0f, t);
            if ((Time.time - startstage_time) > background_fade_time) {
                t = 1f;
                startup_stage = 5;
                startstage_time = Time.time;
                ui.SetActive(false);
            }
            Color newColor = backgroundimage.color;
            newColor.a = a;
            backgroundimage.color = newColor;
        }

        if (startup_stage == 5) {
            Color newColor = backgroundimage.color;
            fractalselectionui.SetActive(true);
            newColor.a = originalbackgroundalpha;
            backgroundimage.color = newColor;
            startup = false;
        }
*/
    }

    // Update is called once per frame
    void Update()
    {
        if (startup) {
            StartupUpdate();
        }
        /*
           image.uvRect.xMax = screenpos.x + screenscale.x;
           image.uvRect.xMin = screenpos.x - screenscale.x;
           image.uvRect.yMax = screenpos.y + screenscale.y;
           image.uvRect.yMin = screenpos.y - screenscale.y;
           */

        int showroot = 0;
        if (!startup && 
                (uiactive || uifadein || uifadeout || fractalfadein || fractalfadeout)) {
            if (fractalfadein || fractalfadeout) {
                FractalFadeUpdate();
            }
            if (uifadein) {
                float t = (Time.time - uichange) / uifadetime;
                canvasGroup.alpha = t;
                if (Time.time - uichange > uifadetime) {
                    uifadein = false;
                    uichange = Time.time;
                    //uiactive = true;
                }
            } else if (uifadeout) {
                float t = (Time.time - uichange) / uifadetime;
                canvasGroup.alpha = 1 - t;
                if (Time.time - uichange > uifadetime) {
                    uifadeout = false;
                    uichange = Time.time;
                    ui.SetActive(false);
                    //uiactive = false;
                }
            } else {
                if (Time.time - uichange > uitime && !uifinished) {
                    UiFinish();
                    uifinished = true;
                } else if (Time.time - uichange < uitime) {
                    UiUpdate();
                    //uiactive = false;
                    //ui.SetActive(false);
                }
            }
        }
        //if (uiactive && Time.time - uichange < 2) {
        //    UiUpdate();
            //uiactive = false;
            //ui.SetActive(false);
        //} 

        bool scint_changed = false;

        // Keyboard zoom 
        if (!startup && (!uiactive && !uifadeout && !uifadein)) {

            if (Input.GetKey("i"))
            {
                scint -= Time.deltaTime;
                scint_changed = true;
            }
            if (Input.GetKey("o"))
            {
                scint += Time.deltaTime;
                scint_changed = true;
            }



            /*
            if (Input.GetKey("k"))
            {
                scint -= Time.deltaTime;
                scint_changed = true;
            }
            if (Input.GetKey("l"))
            {
                scint += Time.deltaTime;
                scint_changed = true;
            }
            */
        }
        // touch zoom
        if (!startup && (!uiactive && !uifadeout && !uifadein)) {
            if (Input.touchCount == 2 && !uiactive && !uifadeout && !uifadein) {

                Touch touch0 = Input.GetTouch(0);
                Touch touch1 = Input.GetTouch(1);

                float currentdistance = Vector2.Distance(touch0.position, touch1.position);
                touchmoving = true;
                touchfading = false;

                //float currentdistance = Vector2.Distance(touch0.position, touch1.position);

                if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began) {
                    initialtouchdistance = currentdistance;
                } else if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved) {
                    // TODO: not sure if this is correct
                    /* Try 1 is okay */
                    float zoomfactor = (currentdistance / initialtouchdistance);
                    scint -= (Time.deltaTime * Mathf.Log(zoomfactor)) * 25f;
                    scint_changed = true;
                    initialtouchdistance = currentdistance;
                    /*
                    float delta = currentdistance - initialtouchdistance * Mathf.Exp(scint) * 0.000070f;
                    scint -= delta;
                    scint_changed = true;
                    */


                    //scint += zoomfactor;
                    //scint_changed = true;
                    /* New scheme!  doesn't work */
                    /*
                    float delta = currentdistance - initialtouchdistance;
                    float zoomfactor = -delta * Mathf.Exp(scint) * 0.0070f;
                    scint += zoomfactor;
                    scint_changed = true;
                    */
                }
            }
        }

        if (!startup && (!uiactive && !uifadeout && !uifadein)) {
            if (Input.GetKey("q"))
            {
                rot += Time.deltaTime;
            }
            if (Input.GetKey("e"))
            {
                rot -= Time.deltaTime;
            }

            if (Input.GetKeyDown(KeyCode.J))
            {
                FractalType = 1 - FractalType;
                keydowncount++;
                //Debug.Log("Set isJulia to " + isJulia + " keydowncount = " + keydowncount);
            }

            if (Input.GetKeyDown(KeyCode.U))
            {
                if (!uifadein && !uifadeout && !fractalfadein && !fractalfadeout) {
                    uiactive = !uiactive;
                    if (uiactive) {
                        SetStatsText();
                        uifadein = true;
                        canvasGroup.alpha = 0;
                        ui.SetActive(true);
                    } else {
                        uifadeout = true;
                        canvasGroup.alpha = 1;
                    }

                    if (uiactive) {
                        UiReset();
                    }
                    //ui.SetActive(uiactive);
                    uichange = Time.time;
                    uifinished = false;
                }
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                ispickover = 1 - ispickover;
                MandleBrot.SetInt("_IsPickover" , ispickover);
            }

            if (Input.GetKey(KeyCode.C))
            {
                showroot = 1;
            }

            if (Input.GetKey(KeyCode.P)) {

                if (Input.GetKey(KeyCode.LeftShift)) {
                    pickoverlinear -= Time.deltaTime;
                } else {
                    pickoverlinear += Time.deltaTime;
                }

                pickoverscale = Mathf.Exp(pickoverlinear);

            } else if (first) {
                pickoverscale = Mathf.Exp(pickoverlinear);
            }
        }


        //screenscale = Mathf.Pow(2.718281828f , scint);
        if (scint_changed || first) {
            screenscale = Mathf.Exp(scint);
            MandleBrot.SetInt("_IsPickover" , ispickover);
        }

        first = false;
        Vector2 move = new Vector2(0 , 0);
        if (!startup && (!uiactive && !uifadeout && !uifadein)) {
            move = new Vector2(Input.GetAxis("Horizontal") * Time.deltaTime , Input.GetAxis("Vertical") * Time.deltaTime) * screenscale * 10;
        }

        if (!startup && (!uiactive && !uifadein && !uifadeout)) {
            if (Input.touchCount == 1) {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began) {
                    touchstart = Time.time;
                    touchmoving = false;
                    touchfading = false;
                }
                if (!touchfading && touch.phase == TouchPhase.Moved) {
                    Vector2 delta = touch.deltaPosition;
                    move = -delta * Mathf.Exp(scint) * 0.0070f;
                    touchmoving = true;
                    touchfading = false;
                    //move = delta * screenscale;
                    //move = touch.deltaPosition * screenscale * 0.01f;
                }
                if (!touchfading && !touchmoving && touch.phase == TouchPhase.Stationary) {
                    if (Time.time - touchstart > longtouchtime) {
                        SetStatsText();
                        uifadein = true;
                        uiactive = true;
                        uichange = Time.time;
                        uifinished = false;
                        canvasGroup.alpha = 0;
                        ui.SetActive(true);
                        touchfading = true;
                        UiReset();
                    }
                }
            }
        }

        move = rotate(move , new Vector2(0 , 0) , rot);

        screenpos += move;
        //Debug.Log("Screenpos is " + screenpos + " move is " + move);
        MandleBrot.SetVector("_Area" , new Vector4(screenpos.x , screenpos.y , screenscale , screenscale));
        MandleBrot.SetFloat("_ColorShift" , shift);
        MandleBrot.SetFloat("_ColorCycle" , cycle);
        MandleBrot.SetFloat("_Rot" , rot);
        MandleBrot.SetFloat("_PickoverScale" , pickoverscale);
        MandleBrot.SetInt("_FractalType" , FractalType);
        MandleBrot.SetVector("_JuliaRoot" , new Vector4(juliaroot.x, juliaroot.y, 0f, 0f));
        MandleBrot.SetInt("_ShowRoot", showroot);

        shift += Time.deltaTime * 1;
    }

    Vector2 rotate(Vector2 pt , Vector2 pv , float ang)
    {
        Vector2 p = pt - pv;
        float s = Mathf.Sin(ang);
        float c = Mathf.Cos(ang);
        p = new Vector2(p.x * c - p.y * s , p.x * s + p.y * c);
        p += pv;
        return p;
    }
}
