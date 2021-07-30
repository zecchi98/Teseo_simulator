using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using UnityEngine;
using System;
using UnityEngine.UI;
public class Player : MonoBehaviour
{
    struct log_struct
    {
        public Vector2 pos;
        public int element_number;
        public bool caduto;
    }
    struct distanza_beacons_struct
    {
        public float beacon_cucina;
        public float beacon_sala;
    }

    int max_length_array_log = 1000000;
    int index_log_pos=0;
    public float moveSpeed;
    public Text textElement;
    public float threshold_mouse_speed;
    public Button ButtonCaduta;
    public Button ButtonSalva;
    public float Durata_caduta=3f;
    public SpriteRenderer beacon_cucina;
    public SpriteRenderer beacon_sala;
    public bool debug_on=false;
    float xInput, yInput;
    Vector2[] log_positions;
    Vector2 targetpos_mouse;
    Vector2 targetpos_arrows;
    Rigidbody2D rb;
    DateTime dt_inizio;
    SpriteRenderer sp;
    Sprite[] spriteArrayUp, spriteArrayLeft, spriteArrayRight,spriteArrayDown;
    bool mouse_click_not_processed = false;
    bool Caduta_in_corso = false;
    double timer_caduta = 0f;
    log_struct[] log_updates;
    distanza_beacons_struct distanza_Beacons;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sp = GetComponent<SpriteRenderer>();
    }
   

    // Start is called before the first frame update
    void Start()
    {

        dt_inizio = DateTime.Now;
        spriteArrayUp = Resources.LoadAll<Sprite>("up");
        spriteArrayLeft = Resources.LoadAll<Sprite>("left");
        spriteArrayRight = Resources.LoadAll<Sprite>("right");
        spriteArrayDown = Resources.LoadAll<Sprite>("down");
        textElement.text = "";
        ButtonCaduta.onClick.AddListener(TaskOnClick_caduta);
        ButtonSalva.onClick.AddListener(TaskOnClick_salva);
        //log_positions = new Vector2[max_length_array_log];
        log_updates = new log_struct[max_length_array_log];
        distanza_Beacons = new distanza_beacons_struct();
        
    }

    // Update is called once per frame
    void Update()
    {


        check_mouse_click();//leave in Update() due to otherwise it will not catch when you release the click




    }
    private void FixedUpdate()
    {//Questa funzione viene eseguita anche se non ci sono update

        
        aggiorna_distanze_beacons();

        check_timer_caduta();
        
        if_arrows_click_move();
  
        if_mouseClick_move();
            
        FlipPlayer();

        update_log();



    }
    void aggiorna_distanze_beacons()
    {
        distanza_Beacons.beacon_cucina = calcola_potenza_beacon(beacon_cucina);
        distanza_Beacons.beacon_sala = calcola_potenza_beacon(beacon_sala);

    }
    bool if_arrows_click_move()
    {

        if (Caduta_in_corso)
            return false;

        xInput = Input.GetAxis("Horizontal");
        yInput = Input.GetAxis("Vertical");
        if (Math.Abs(xInput) < 0.3f)
            xInput = 0;
        if (Math.Abs(yInput) < 0.3f)
            yInput = 0;

        if (xInput == 0f && yInput == 0f)
            return false;

        transform.Translate(xInput * moveSpeed * Time.deltaTime, yInput * moveSpeed * Time.deltaTime, 0);
        return true;

    }
    bool check_mouse_click()
    {
        if (Caduta_in_corso)
            return false;

        
        //Mouse control:
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 10f;
        if (Input.GetMouseButtonDown(0))
        {
            targetpos_mouse = mousePos;
           mouse_click_not_processed = true;
        }
        if (Input.GetMouseButtonUp(0))
        {
           mouse_click_not_processed = false;
        }
        
        return true;
    }
    bool if_mouseClick_move()
    {
        if (!mouse_click_not_processed)
            return false;

        textElement.text = targetpos_mouse.x.ToString() + "-" + targetpos_mouse.y.ToString();
        if (targetpos_mouse.x > 7.8f || targetpos_mouse.y > 2f || targetpos_mouse.y < -5.4f || targetpos_mouse.x < -5f)
        {
            mouse_click_not_processed = false;
            return false;
            
        }

        transform.position = Vector3.MoveTowards(transform.position, targetpos_mouse, moveSpeed*Time.deltaTime);
        return true;
    }
    void update_log()
    {
        if (index_log_pos < max_length_array_log)
        {
            //log_positions[index_log_pos] = transform.position;
            log_updates[index_log_pos].pos = transform.position;
            log_updates[index_log_pos].element_number = index_log_pos;
            log_updates[index_log_pos].caduto = Caduta_in_corso;

            index_log_pos++;
        }
        else
        {
            textElement.text = "Max index reached";
        }
    }
    void FlipPlayer()
    {
        
        if (!mouse_click_not_processed)
        {

            if (xInput < -0f)
            {
                sp.sprite = spriteArrayLeft[0];
            }
            else if (xInput > 0f)
            {
                sp.sprite = spriteArrayRight[0];
            }

            if (yInput < -0f)
            {
                sp.sprite = spriteArrayDown[0];
            }
            else if (yInput > 0f)
            {
                sp.sprite = spriteArrayUp[0];
            }
        }
        else
        {
            float err_x, err_y, t_x, t_y;
            //transform.position = Vector2.MoveTowards(transform.position, targetpos, 0.3f);
            err_x = targetpos_mouse.x - transform.position.x;
            err_y = targetpos_mouse.y - transform.position.y;

            if (Math.Abs(err_x) > Math.Abs(err_y))
            {

                if (err_x > 0f)
                {
                    sp.sprite = spriteArrayRight[0];
                }
                if (err_x < 0f)
                {
                    sp.sprite = spriteArrayLeft[0];
                }
            }
            else
            {
                if (err_y > 0f)
                {
                    sp.sprite = spriteArrayUp[0];
                }
                if (err_y < 0f)
                {
                    sp.sprite = spriteArrayDown[0];
                }


            }
        }





    }
    
    float distanza_euclidea(Vector3 a,Vector3 b)
    {
        float d_x = (a.x - b.x);
        float d_y = (a.y - b.y);
        return (float)(Math.Sqrt((double)(d_x * d_x + d_y * d_y)));
    }
    Vector2 direzione_tra_due_punti(Vector3 a,Vector3 b)
    {
        return b-a;
    }
    float calcola_potenza_beacon(SpriteRenderer beacon)
    {
        float K=1,dist_beacon;
        int cont=0;
        
        print_debug("Calcolo la distanza da beacon: " + beacon.name);
        dist_beacon = distanza_euclidea(beacon.transform.position, transform.position);
        Vector2 dir_ = direzione_tra_due_punti(transform.position, beacon.transform.position);
        if(debug_on)    
            Debug.DrawRay(transform.position, dir_ * 10f, Color.red);
        RaycastHit2D[] hit = new RaycastHit2D[10];
        hit = Physics2D.RaycastAll(transform.position, dir_, dist_beacon);

        //stampa_su_debug1(distanza_Beacons.beacon_cucina.ToString());
        for (int i = 0; i < hit.Length; i++)
        {

            if (hit[i])
            {
                if (hit[i].collider.name != beacon.name && hit[i].collider.name != "Player")
                {
                    print_debug(i.ToString() + "- Collisione:" + hit[i].collider.name);
                    cont++;
                }

            }
        }
        K = 1 - 0.1f * cont;

        print_debug("Oggetti esterni rilevati:" + cont.ToString());
        print_debug("Distanza effettiva:" + dist_beacon);
        print_debug("Distanza con rumore:" + dist_beacon * K);
        return dist_beacon * K;  
        
    }
    
    void check_timer_caduta()
    {
        if (Caduta_in_corso)
        {
            //Timer
            if (timer_caduta < Durata_caduta)
            {
                timer_caduta += Time.deltaTime;
            }
            else
            {
                Caduta_in_corso = false;
                timer_caduta = 0;
                textElement.text = "";
            }
        }

    }
    void TaskOnClick_caduta()
    {
        Debug.Log("You have clicked the button!");
        Caduta_in_corso = true; 
        timer_caduta = 0;
        stampa_su_debug1("Caduto");
        mouse_click_not_processed = false;
    }
    void TaskOnClick_salva()
    {
        if (!Caduta_in_corso)
        {
            Debug.Log("Salvataggio in corso");
            crea_e_salva_xml();
            Debug.Log("Salvataggio effettuato");
        }
        else
        {
            Debug.Log("Non puoi salvare mentre una caduta è in corso");

        }
    }

    //ELIMINARE POSSIBILITA' DI CLICCARE NELLA ZONA DEI PULSANTI
    void crea_e_salva_xml()
    {
        DateTime dt = DateTime.Now;

        TimeSpan t_durata_script= dt.Subtract(dt_inizio);


        string data_str=String.Format("{0:d-M-yyyy HH-mm-ss}", dt);
        string filename = "C:\\Users\\zecch\\Desktop\\Zeck\\TESEO\\Documenti\\XML_OUTPUT\\";
        filename = filename + data_str + ".xml";
        XmlTextWriter xmlTextWriter = new XmlTextWriter(filename, System.Text.Encoding.UTF8);
        xmlTextWriter.Formatting = Formatting.Indented;
        xmlTextWriter.WriteStartDocument();
        xmlTextWriter.WriteComment("Creating an xml file using C#");

        xmlTextWriter.WriteStartElement("UnityDebug");

        xmlTextWriter.WriteElementString("Num Eventi Registrati", index_log_pos.ToString());

        xmlTextWriter.WriteStartElement("Data Registrazione");
        xmlTextWriter.WriteElementString("Year", dt.Year.ToString());
        xmlTextWriter.WriteElementString("Month", dt.Month.ToString());
        xmlTextWriter.WriteElementString("Day", dt.Day.ToString());
        xmlTextWriter.WriteElementString("Hour", dt.Hour.ToString());
        xmlTextWriter.WriteElementString("Minute", dt.Minute.ToString());
        xmlTextWriter.WriteElementString("Second", dt.Second.ToString());
        xmlTextWriter.WriteEndElement();

        xmlTextWriter.WriteStartElement("Durata Registrazione");
        xmlTextWriter.WriteElementString("Days", t_durata_script.Days.ToString());
        xmlTextWriter.WriteElementString("Hours", t_durata_script.Hours.ToString());
        xmlTextWriter.WriteElementString("Minutes", t_durata_script.Minutes.ToString());
        xmlTextWriter.WriteElementString("Seconds", t_durata_script.Seconds.ToString());
        xmlTextWriter.WriteElementString("Milliseconds", t_durata_script.Milliseconds.ToString());
        xmlTextWriter.WriteEndElement();

        xmlTextWriter.WriteStartElement("Eventi");

        if (log_updates.Length != 0)
        {
            for (int i = 0; i < index_log_pos; i++)
            {
                xmlTextWriter.WriteStartElement("Evento");
                /*
                xmlTextWriter.WriteStartElement("Position");
                xmlTextWriter.WriteElementString("x", log_positions[i].x.ToString());
                xmlTextWriter.WriteElementString("y", log_positions[i].y.ToString());
                xmlTextWriter.WriteEndElement();*/

                xmlTextWriter.WriteElementString("Element number", log_updates[i].element_number.ToString());
                xmlTextWriter.WriteElementString("Caduta in corso", log_updates[i].caduto.ToString());

                xmlTextWriter.WriteStartElement("Position");
                xmlTextWriter.WriteElementString("x", log_updates[i].pos.x.ToString());
                xmlTextWriter.WriteElementString("y", log_updates[i].pos.y.ToString());
                xmlTextWriter.WriteEndElement(); 

                
                
                xmlTextWriter.WriteEndElement();
            }
        }
        xmlTextWriter.WriteEndElement();
        xmlTextWriter.WriteEndElement();
        xmlTextWriter.WriteEndDocument();
        xmlTextWriter.Flush();//terminate stream of bytes
        xmlTextWriter.Close();


    }
    void stampa_su_debug1(string x)
    {
        textElement.text = x;
    }
    void print_debug(string s)
    {
        if (debug_on)
        {
            Debug.Log(s);
        }
    }
}
