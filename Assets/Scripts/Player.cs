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

    int max_length_array_log = 1000000;
    int index_log_pos=0;
    public float moveSpeed;
    public Text textElement;
    public float threshold_mouse_speed;
    public Button ButtonCaduta;
    public Button ButtonSalva;
    public float Durata_caduta=3f;


    float xInput, yInput;
    Vector2[] log_positions;
    Vector2 targetpos;
    Rigidbody2D rb;
    DateTime dt_inizio;
    SpriteRenderer sp;
    Sprite[] spriteArrayUp, spriteArrayLeft, spriteArrayRight,spriteArrayDown;
    bool mouse_click_not_processed = false;
    bool Caduta_in_corso = false;
    double timer_caduta = 0f;
    log_struct[] log_updates;


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
        
    }

    // Update is called once per frame
    void Update()
    {
        //Mouse control:
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 10f;

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
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                targetpos = mousePos;
                mouse_click_not_processed = true;
            }
            if (Input.GetMouseButtonUp(0))
            {
                mouse_click_not_processed = false;
            }
        }

        

    }
    private void FixedUpdate()
    {

        if (!Caduta_in_corso)
        {
            //Questa funzione viene eseguita anche se non ci sono update
            xInput = Input.GetAxis("Horizontal");
            yInput = Input.GetAxis("Vertical");


            transform.Translate(xInput * moveSpeed, yInput * moveSpeed, 0);

            if (mouse_click_not_processed)
            {
                ClickToMove();
                //mouse_click_not_processed = false;
            }



            PlatformerMove();


            FlipPlayer();
        }

        if (index_log_pos< max_length_array_log)
        {
            //log_positions[index_log_pos] = transform.position;
            log_updates[index_log_pos].pos = transform.position;
            log_updates[index_log_pos].element_number = index_log_pos;
            log_updates[index_log_pos].caduto=Caduta_in_corso;

            index_log_pos++;
        }
        else
        {
            textElement.text = "Max index reached";
        }

    }
    bool ClickToMove()
    {
        if (targetpos.x > 4f || targetpos.x < -4f )
        {
            mouse_click_not_processed = false;
            return false;
            
        }
        /*if (targetpos.y > 200f || targetpos.y < -600f)
        {
            return;
        }*/

        float err_x, err_y,t_x,t_y;
        //transform.position = Vector2.MoveTowards(transform.position, targetpos, 0.3f);
        err_x = targetpos.x - transform.position.x;
        err_y = targetpos.y - transform.position.y;
        
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

        /*
        t_x = err_x * moveSpeed;
        t_y = err_y * moveSpeed;
        

        if (t_x > threshold_mouse_speed)
        {
            t_x = threshold_mouse_speed;
        }
        if (t_x < -threshold_mouse_speed)
        {
            t_x = -threshold_mouse_speed;
        }
        if (t_y> threshold_mouse_speed)
        {
            t_y = threshold_mouse_speed;
        }
        if (t_y < -threshold_mouse_speed)
        {
            t_y = -threshold_mouse_speed;
        }

        */
        //transform.Translate(t_x, t_y, 0);
        transform.position = Vector3.MoveTowards(transform.position, targetpos, moveSpeed*Time.deltaTime);
        return true;
    }

    void PlatformerMove()
    {
        rb.velocity = new Vector2(moveSpeed * xInput, moveSpeed * yInput);

    }

    void FlipPlayer()
    {
        if(rb.velocity.x < -0f)
        {
            sp.sprite = spriteArrayLeft[0];
        }
        else if (rb.velocity.x > 0f)
        {
            sp.sprite = spriteArrayRight[0];
        }

        if (rb.velocity.y < -0f)
        {
            sp.sprite = spriteArrayDown[0];
        }
        else if (rb.velocity.y > 0f)
        {
            sp.sprite = spriteArrayUp[0];
        }
    }
    
  
    void TaskOnClick_caduta()
    {
        Debug.Log("You have clicked the button!");
        Caduta_in_corso = true; 
        timer_caduta = 0;
        textElement.text = "Caduto";
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
}
