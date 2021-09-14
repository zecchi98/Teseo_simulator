
using System.Xml.Schema;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UnityEngine;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    struct log_struct
    {
        public Vector2 pos;
        public int element_number;
        public bool caduto;
        //List<beacon> Beacons;
    }


    public const int numero_distanze_raccolte_calibrazione = 5000;
    int max_length_array_log = 1000000;
    public float Durata_caduta = 3f;
    float Durata_set_calibrazione = 5f;
    float limit_max_power_beacon = 1, point_of_change_power_beacon=3.47f;

    Beacons Beacons_list;

    //int index_log_pos=0;
    public float moveSpeed;
    public Text textElement;
    public float threshold_mouse_speed;

    public Button ButtonCaduta;
    public Button ButtonSalva;
    public Button Button_Calibrazione;
    public Button Btn_calibra_stanza;
    public Button Btn_Train;
    public bool parla_con_server = true;
    Button Btn_inattivita,Btn_localizzati,Btn_elimina_calibrazione;
    //public SpriteRenderer beacon_cucina;
    //public SpriteRenderer beacon_sala;
    //public SpriteRenderer beacon_camera;
    //List<SpriteRenderer> beacons_sprites;
    public bool debug_on = false;
    float xInput, yInput;
    Vector2[] log_positions;
    Vector2 targetpos_mouse;
    Vector2 targetpos_arrows;
    Rigidbody2D rb;
    DateTime dt_inizio, dt_cont_orologio, dt_cont_beacon;
    SpriteRenderer sp;
    Sprite[] spriteArrayUp, spriteArrayLeft, spriteArrayRight, spriteArrayDown;
    double timer_caduta = 0f;
    log_struct[] log_updates;

    int total_calibration_wifi_sent = 0;

    bool message_to_be_analyzed = false;
    bool msg_from_get_area = false;
    bool predisporre_calibrazione = false;
    bool mouse_click_not_processed = false;
    bool Caduta_in_corso = false;
    Classe_calibrazione calibrazione_class;
    Areas areas_list;
    public Dropdown dropdown_calibrazione;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sp = GetComponent<SpriteRenderer>();
    }

    void Start()
    {

        dt_inizio = DateTime.Now;
        dt_cont_orologio = DateTime.Now;
        dt_cont_beacon = DateTime.Now;
        spriteArrayUp = Resources.LoadAll<Sprite>("up");
        spriteArrayLeft = Resources.LoadAll<Sprite>("left");
        spriteArrayRight = Resources.LoadAll<Sprite>("right");
        spriteArrayDown = Resources.LoadAll<Sprite>("down");
        textElement.text = "";
        ButtonCaduta.onClick.AddListener(TaskOnClick_caduta);
        ButtonSalva.onClick.AddListener(TaskOnClick_salva);
        Button_Calibrazione.onClick.AddListener(TaskOnClick_Calibrazione);
        //log_positions = new Vector2[max_length_array_log];
        log_updates = new log_struct[max_length_array_log];
        //distanza_Beacons = new distanza_beacons_struct();
        Beacons_list = new Beacons();
        calibrazione_class = new Classe_calibrazione();
        Btn_inattivita = GameObject.Find("Inattivita_Btn").GetComponent<Button>();
        Btn_inattivita.onClick.AddListener(TaskOnClick_inattivita);
        Btn_elimina_calibrazione = GameObject.Find("Elimina_calibrazione_Btn").GetComponent<Button>();
        Btn_elimina_calibrazione.onClick.AddListener(elimina_tutte_calibrazioni);

        areas_list = new Areas();
        SpriteRenderer[] gg = GameObject.Find("Beacons_folder").GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer g in gg)
        {
            //Debug.Log(g.name);
            Beacons_list.Beacons_list.Add(g);
        }
        //Debug.Log(Beacons_list);

        Btn_localizzati = GameObject.Find("Localizzati_Btn").GetComponent<Button>();
        Btn_localizzati.onClick.AddListener(localizzati);

        chiama_get_area_list();
        chiama_orologio();

    }

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

        //update_log();

        controlla_orologio();

        controlla_e_predisponi_calibrazione();

        controlla_ed_invia_calibrazione();
    }

    void controlla_orologio()
    {

        DateTime dt = DateTime.Now;
        TimeSpan t_ = dt.Subtract(dt_cont_orologio);
        if (t_.Seconds > 10)
        {

            chiama_orologio();
        }
    }
    void chiama_orologio()
    {
        dt_cont_orologio = DateTime.Now;
        string chiave_orologio = "b235d3df-7e33-4533-b750-febc8a8dd931";
        string install_prefix = "70c0637bba8bd6168c03f8dd803ffecc";
        string site_domain = "kibitest.cloud";
        string edge_url = "https://" + install_prefix + "-edge." + site_domain + "/api";
        string uri = edge_url + "/v1/device_heartbeat/" + chiave_orologio;



        orologio_request orologio_obj = new orologio_request(60, "federico-sim");

        string bodyJsonString = Newtonsoft.Json.JsonConvert.SerializeObject(orologio_obj);
        //Debug.Log("Json:"+bodyJsonString);

        //PostData(uri, bodyJsonString);

        if(parla_con_server)
            StartCoroutine(PostData_Coroutine(uri, bodyJsonString, (value) => { Response_from_server(value); }));

    }
    void chiama_server_caduta()
    {
        string chiave_orologio = "b235d3df-7e33-4533-b750-febc8a8dd931";
        string install_prefix = "70c0637bba8bd6168c03f8dd803ffecc";
        string site_domain = "kibitest.cloud";
        string edge_url = "https://" + install_prefix + "-edge." + site_domain + "/api";
        string uri = edge_url + "/v1/alert";


        TimeSpan mill_seconds_total = DateTime.Now.Subtract(dt_inizio);
        alert_request alert_caduta = new alert_request("FALL_DETECTED", chiave_orologio, mill_seconds_total.TotalMilliseconds);

        string bodyJsonString = Newtonsoft.Json.JsonConvert.SerializeObject(alert_caduta);
        Debug.Log("bodyJsonString:" + bodyJsonString);



        if (parla_con_server)
            StartCoroutine(PostData_Coroutine(uri, bodyJsonString, (value) => { Response_from_server(value); }));

    }
    void chiama_server_inactivity()
    {
        string chiave_orologio = "b235d3df-7e33-4533-b750-febc8a8dd931";
        string install_prefix = "70c0637bba8bd6168c03f8dd803ffecc";
        string site_domain = "kibitest.cloud";
        string edge_url = "https://" + install_prefix + "-edge." + site_domain + "/api";
        string uri = edge_url + "/v1/alert";


        TimeSpan mill_seconds_total = DateTime.Now.Subtract(dt_inizio);
        alert_request alert_caduta = new alert_request("INACTIVITY_DETECTED", chiave_orologio, mill_seconds_total.TotalMilliseconds);

        string bodyJsonString = Newtonsoft.Json.JsonConvert.SerializeObject(alert_caduta);
        Debug.Log("bodyJsonString:" + bodyJsonString);



        if (parla_con_server)
            StartCoroutine(PostData_Coroutine(uri, bodyJsonString, (value) => { Response_from_server(value); }));

    }
    void chiama_get_area_list()
    {
        string install_prefix = "70c0637bba8bd6168c03f8dd803ffecc";
        string site_domain = "kibitest.cloud";
        string edge_url = "https://" + install_prefix + "-loc." + site_domain + "/api";
        string uri = edge_url + "/v1/get_area_list";




        if (parla_con_server)
            StartCoroutine(GetData_Coroutine(uri, (value) => { Response_from_get_area_list(value); }));
    }
    void aggiorna_distanze_beacons()
    {
        DateTime dt = DateTime.Now;
        TimeSpan t_ = dt.Subtract(dt_cont_beacon);
        if (t_.Milliseconds > 0)
        {
            dt_cont_beacon = DateTime.Now;
            wifi_group wifi_Group = new wifi_group();
            foreach (SpriteRenderer beacon_obj in Beacons_list.Beacons_list)
            {
                wifi_entry wifi_Entry = new wifi_entry();
                float distance = calcola_potenza_beacon(beacon_obj);

                wifi_Entry.wifi_level = (int)distance;
                wifi_Entry.wifi_name = beacon_obj.name;

                wifi_Group.wifi_group_.Add(wifi_Entry);
            }
            if (calibrazione_class.calibrazione_avviata)
            {
                if (calibrazione_class.index_distanze >= numero_distanze_raccolte_calibrazione)
                {
                    calibrazione_class.calibrazione_avviata = false;
                    Debug.Log("Ho raccolto troppi dati");
                }
                else
                {
                    calibrazione_class.incorpora_nuovo_gruppo(wifi_Group);
                }

            }
        }

    }
    void chiama_clear_scan_data(string nome_area)
    {
        //Debug.Log("Sto eliminando i dati presenti nella calibrazione");
        string install_prefix = "70c0637bba8bd6168c03f8dd803ffecc";
        string site_domain = "kibitest.cloud";
        string edge_url = "https://" + install_prefix + "-loc." + site_domain + "/api";
        string uri = edge_url + "/v1/clear_scan_data";

        Oggetto_id_area_for_server t = new Oggetto_id_area_for_server();
        t.id = areas_list.cerca_id_tramite_nome(nome_area);
        string bodyJsonString = Newtonsoft.Json.JsonConvert.SerializeObject(t);

        //Debug.Log("Sending to:" + uri);
        //Debug.Log("bodyJsonString:" + bodyJsonString);



        if (parla_con_server)
            StartCoroutine(PostData_Coroutine(uri, bodyJsonString, (value) => { Response_from_server(value); }));

    }
    void chiama_traning()
    {
        string install_prefix = "70c0637bba8bd6168c03f8dd803ffecc";
        string site_domain = "kibitest.cloud";
        string edge_url = "https://" + install_prefix + "-loc." + site_domain + "/api";
        string uri = edge_url + "/v1/training";




        if (parla_con_server)
            StartCoroutine(GetData_Coroutine(uri, (value) => { Response_from_server(value); }));

    }
    void elimina_tutte_calibrazioni()
    {
        chiama_clear_scan_data("Cucina");
        chiama_clear_scan_data("Sala");
        chiama_clear_scan_data("Camera da letto");
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

        transform.position = Vector3.MoveTowards(transform.position, targetpos_mouse, moveSpeed * Time.deltaTime);
        return true;
    }
    /*void update_log()
    {
        if (index_log_pos < max_length_array_log)
        {
            //log_positions[index_log_pos] = transform.position;
            log_updates[index_log_pos].pos = transform.position;
            log_updates[index_log_pos].element_number = index_log_pos;
            log_updates[index_log_pos].caduto = Caduta_in_corso;
            log_updates[index_log_pos].distanza_beacons =distanza_Beacons;

            index_log_pos++;
        }
        else
        {
            textElement.text = "Max index reached";
        }
    }
    */
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
            float err_x, err_y;
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

    float distanza_euclidea(Vector3 a, Vector3 b)
    {
        float d_x = (a.x - b.x);
        float d_y = (a.y - b.y);
        return (float)(Math.Sqrt((double)(d_x * d_x + d_y * d_y)));
    }
    Vector2 direzione_tra_due_punti(Vector3 a, Vector3 b)
    {
        return b - a;
    }
    float calcola_potenza_beacon(SpriteRenderer beacon_obj)
    {

        /*
         * y=10   y=f-0.1x  con x=limite  ==> f=10+0.1*x
         * 
         *
         */


        SpriteRenderer beacon = beacon_obj;

        float K = 1, gain = 100000, dist_beacon,dist_euclidea,potenza=0, potenza_con_rumore;
        int cont = 0;

        print_debug("Calcolo la distanza da beacon: " + beacon.name);
        dist_euclidea = distanza_euclidea(beacon.transform.position, transform.position);
        Vector2 dir_ = direzione_tra_due_punti(transform.position, beacon.transform.position);
        if (debug_on)
            Debug.DrawRay(transform.position, dir_ * 10f, Color.red);
        RaycastHit2D[] hit = new RaycastHit2D[10];
        hit = Physics2D.RaycastAll(transform.position, dir_, dist_euclidea);

        //stampa_su_debug1(distanza_Beacons.beacon_cucina.ToString());
        for (int i = 0; i < hit.Length; i++)
        {

            if (hit[i])
            {
                if (hit[i].collider.name != beacon.name && hit[i].collider.name != "Player")
                {
                    //print_debug(i.ToString() + "- Collisione:" + hit[i].collider.name);
                    cont++;
                }

            }
        }

        //Rendo la distanza non lineare

        if (dist_euclidea > limit_max_power_beacon && dist_euclidea < point_of_change_power_beacon )
        {
            potenza = (10.1f - 0.1f*dist_euclidea);
        }
        if (dist_euclidea > point_of_change_power_beacon)
        {
            potenza = (33.84f / dist_euclidea);
        }
        if (dist_euclidea < limit_max_power_beacon)
        {
            potenza = 10f;
        }
        potenza = potenza * gain;

        K = 1 - 0.3f * cont;
        potenza_con_rumore = potenza * K;

        if (potenza_con_rumore < 200000f)
            potenza_con_rumore = 0f;

        

        print_debug("Oggetti esterni rilevati:" + cont.ToString());
        print_debug("Distanza effettiva:" + dist_euclidea);
        print_debug("Potenza effettiva" + potenza);
        print_debug("Potenza con rumore" + potenza_con_rumore);
        return potenza_con_rumore;

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
        chiama_server_caduta();
    }
    void TaskOnClick_salva()
    {
        //localizzati();
        /*
        if (!Caduta_in_corso)
        {
            Debug.Log("Salvataggio in corso");
            //crea_e_salva_xml();
            Debug.Log("Salvataggio effettuato");
        }
        else
        {
            Debug.Log("Non puoi salvare mentre una caduta è in corso");

        }
        */
    }
    void TaskOnClick_Calibrazione()
    {



        if (msg_from_get_area)
        {
            predisporre_calibrazione = true;

            Debug.Log("Menu_calibrazione Attivato");
            SceneManager.LoadScene("Menu_calibrazione", LoadSceneMode.Additive);

        }

    }

    void Task_drdn_calibra_changed()
    {
        //Debug.Log(dropdown_calibrazione.value);
    }

    void TaskOnClick_inattivita()
    {
        Debug.Log("Inattività detected");
        chiama_server_inactivity();
    }

    void TaskOnClick_calibra_stanza()
    {

        if (calibrazione_class.calibrazione_avviata)
        {
            calibrazione_class.calibrazione_avviata = false;
            chiama_ed_invia_calibrazione();
            total_calibration_wifi_sent = 0;
            Debug.Log("Menu_calibrazione Disattivato");
            SceneManager.UnloadSceneAsync("Menu_calibrazione");

        }
        else
        {
            string stanza = dropdown_calibrazione.options[dropdown_calibrazione.value].text;
            string id_stanza = areas_list.cerca_id_tramite_nome(stanza);
            Debug.Log("Sto per eseguire calibrazione camera:" + stanza + ";" + id_stanza);
            //SceneManager.UnloadSceneAsync("Menu_calibrazione");


            calibrazione_class = new Classe_calibrazione();
            calibrazione_class.inizializza(id_stanza);
            Btn_calibra_stanza.GetComponentInChildren<Text>().text = "Stop Calibrazione";
        }

    }
    void Task_OnClick_Train()
    {
        Debug.Log("Inizio Train");
        chiama_traning();
    }
    bool controlla_e_predisponi_calibrazione()
    {


        if (predisporre_calibrazione) {
            predisporre_calibrazione = false;



            if (!SceneManager.GetSceneByName("Menu_calibrazione").IsValid())
                return false;
            if (!GameObject.Find("Canvas_calibrazione"))
                return false;

            //Inizializzo Dropdown Menu stanze
            {
                //dropdown_calibrazione = GameObject.Find("Canvas_calibrazione").GetComponentInChildren<Dropdown>();
                dropdown_calibrazione = GameObject.Find("Dropdown_nome_stanza").GetComponent<Dropdown>();
                dropdown_calibrazione.onValueChanged.AddListener(delegate { Task_drdn_calibra_changed(); });
            }

            //Inizializzo Buttoni di calibrazione
            {
                Button[] Buttonss = GameObject.Find("Canvas_calibrazione").GetComponentsInChildren<Button>();
                
                foreach (Button b in Buttonss)
                {
                   if (b.name == "Btn_Calibra_Stanza")
                   {
                            Btn_calibra_stanza = b;
                   }
                    if (b.name == "Btn_Train")
                    {
                        Btn_Train = b;

                    }
                }
                Btn_calibra_stanza.onClick.AddListener(delegate { TaskOnClick_calibra_stanza(); });
                Btn_calibra_stanza.GetComponentInChildren<Text>().text = "Start Calibrazione";
                Btn_Train.onClick.AddListener(delegate { Task_OnClick_Train(); });
            }



            //Sistemo dropdown menu stanze
            {
                int diff_array = 0;
                diff_array = dropdown_calibrazione.options.Capacity - (areas_list.areas.Capacity - 1);

                if (diff_array != 0)
                {
                    if (diff_array < 0)
                    {

                        for (int j = 0; j < -diff_array; j++)
                        {
                            Dropdown.OptionData item_copy = new Dropdown.OptionData();
                            item_copy.text = dropdown_calibrazione.options[0].text;
                            item_copy.image = dropdown_calibrazione.options[0].image;
                            dropdown_calibrazione.options.Add(item_copy);
                        }
                    }
                    if (diff_array > 0)
                    {
                        Debug.Log("Areas capacity:" + areas_list.areas.Capacity);
                        Debug.Log("Dropdown options:" + dropdown_calibrazione.options.Capacity);
                        Debug.LogError("");
                    }
                }

                int i = 0;
                foreach (Area area in areas_list.areas)
                {

                    dropdown_calibrazione.options[i].text = area.area;

                    i = i + 1;
                }
                dropdown_calibrazione.RefreshShownValue();
            }


        }
        else
        {
            return false;
        }
        return true;
    }
    void controlla_ed_invia_calibrazione()
    {
        if (calibrazione_class.calibrazione_avviata)
        {
            TimeSpan x = DateTime.Now.Subtract(calibrazione_class.dt_inizio);
            if (x.Seconds >= Durata_set_calibrazione)
            {
                Classe_calibrazione old = calibrazione_class;
                chiama_ed_invia_calibrazione();
                calibrazione_class = new Classe_calibrazione();
                calibrazione_class.inizializza(old.area_id);
            }

        }
    }

    /* void crea_e_salva_xml()
     {
         int max_length_file_position = 100;

         int cont_num_file_pos = 1;
         int t = index_log_pos;
         do {
             t = t - max_length_file_position;
             if (t > 0)
             {
                 cont_num_file_pos++;
             }
         } while (t>0);


         DateTime dt = DateTime.Now;

         TimeSpan t_durata_script= dt.Subtract(dt_inizio);

         string data_str=String.Format("{0:dd-MM-yyyy HH-mm-ss}", dt);
         string filename = "C:\\Users\\zecch\\Desktop\\Zeck\\TESEO\\Documenti\\XML_OUTPUT\\";
         filename = filename + data_str;
         string main_filename = filename + " - main.xml";
         string pre_filename_pos = filename + " - pos ";
         XmlTextWriter xmlTextWriter = new XmlTextWriter(main_filename, System.Text.Encoding.UTF8);
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




         xmlTextWriter.WriteStartElement("FILES");
         for(int i = 1; i <= cont_num_file_pos; i++)
         {
             xmlTextWriter.WriteElementString("Posizioni " + i.ToString(), pre_filename_pos + i.ToString() + ".xml");
         }

         xmlTextWriter.WriteEndElement();

         xmlTextWriter.WriteEndElement();//Unity debug
         xmlTextWriter.WriteEndDocument();
         xmlTextWriter.Flush();//terminate stream of bytes
         xmlTextWriter.Close();

         //FILE EVENTI
         for (int num_file_pos = 1; num_file_pos <= cont_num_file_pos; num_file_pos++)
         {
             xmlTextWriter = new XmlTextWriter(pre_filename_pos + num_file_pos.ToString() + ".xml", System.Text.Encoding.UTF8);
             xmlTextWriter.Formatting = Formatting.Indented;
             xmlTextWriter.WriteStartDocument();
             xmlTextWriter.WriteComment("Creating an xml file using C#");

             xmlTextWriter.WriteStartElement("UnityDebug");

             xmlTextWriter.WriteElementString("Num Eventi Registrati", index_log_pos.ToString()); //da cambiare con index giusto

             xmlTextWriter.WriteStartElement("Data Registrazione");
             xmlTextWriter.WriteElementString("Year", dt.Year.ToString());
             xmlTextWriter.WriteElementString("Month", dt.Month.ToString());
             xmlTextWriter.WriteElementString("Day", dt.Day.ToString());
             xmlTextWriter.WriteElementString("Hour", dt.Hour.ToString());
             xmlTextWriter.WriteElementString("Minute", dt.Minute.ToString());
             xmlTextWriter.WriteElementString("Second", dt.Second.ToString());
             xmlTextWriter.WriteEndElement();

             xmlTextWriter.WriteStartElement("Durata Registrazione"); // da aggiornare con durata effettiva del file
             xmlTextWriter.WriteElementString("Days", t_durata_script.Days.ToString());
             xmlTextWriter.WriteElementString("Hours", t_durata_script.Hours.ToString());
             xmlTextWriter.WriteElementString("Minutes", t_durata_script.Minutes.ToString());
             xmlTextWriter.WriteElementString("Seconds", t_durata_script.Seconds.ToString());
             xmlTextWriter.WriteElementString("Milliseconds", t_durata_script.Milliseconds.ToString());
             xmlTextWriter.WriteEndElement();




             xmlTextWriter.WriteStartElement("FILES");

             xmlTextWriter.WriteElementString("Main", filename + " - main.xml");

             for (int k = 1; k <= cont_num_file_pos; k++)
             {
                 xmlTextWriter.WriteElementString("Posizioni " + k.ToString(), pre_filename_pos + k.ToString() + ".xml");
             }

             xmlTextWriter.WriteEndElement();

             xmlTextWriter.WriteStartElement("Eventi");

             if (log_updates.Length != 0)
             {
                 int end_of_array=0;
                 int start_of_array = 0;

                 start_of_array = (num_file_pos - 1) * max_length_file_position;

                 if (num_file_pos==cont_num_file_pos)
                 {
                     end_of_array = index_log_pos;
                 }
                 else
                 {
                     end_of_array = start_of_array + max_length_file_position;
                 }

                 for (int i = start_of_array; i < end_of_array; i++)
                 {
                     xmlTextWriter.WriteStartElement("Evento");

                     xmlTextWriter.WriteElementString("Element number", log_updates[i].element_number.ToString());
                     xmlTextWriter.WriteElementString("Caduta in corso", log_updates[i].caduto.ToString());

                     xmlTextWriter.WriteStartElement("Position");
                     xmlTextWriter.WriteElementString("x", log_updates[i].pos.x.ToString());
                     xmlTextWriter.WriteElementString("y", log_updates[i].pos.y.ToString());
                     xmlTextWriter.WriteEndElement();


                     xmlTextWriter.WriteStartElement("Distanza Beacons");
                     xmlTextWriter.WriteElementString("Cucina", log_updates[i].distanza_beacons.beacon_cucina.ToString());
                     xmlTextWriter.WriteElementString("Sala", log_updates[i].distanza_beacons.beacon_sala.ToString());
                     xmlTextWriter.WriteEndElement();

                     xmlTextWriter.WriteEndElement();
                 }
             }
             xmlTextWriter.WriteEndElement();//chiude eventi

             xmlTextWriter.WriteEndElement();//Unity debug
             xmlTextWriter.WriteEndDocument();
             xmlTextWriter.Flush();//terminate stream of bytes
             xmlTextWriter.Close();

         }


     }
     */

    void localizzati()
    {
        Debug.Log("Chiamo server per localizzazione");

        string install_prefix = "70c0637bba8bd6168c03f8dd803ffecc";
        string site_domain = "kibitest.cloud";
        string edge_url = "https://" + install_prefix + "-loc." + site_domain + "/api";
        string uri = edge_url + "/v1/predict";

        string chiave_orologio = "b235d3df-7e33-4533-b750-febc8a8dd931";


        Classe_localizzazione result = new Classe_localizzazione();

        wifi_group wifi_Group = new wifi_group();
        foreach (SpriteRenderer beacon_obj in Beacons_list.Beacons_list)
        {
            wifi_entry wifi_Entry = new wifi_entry();
            float distance = calcola_potenza_beacon(beacon_obj);

            wifi_Entry.wifi_level = (int)distance;
            wifi_Entry.wifi_name = beacon_obj.name;

            wifi_Group.wifi_group_.Add(wifi_Entry);
        }

        result.data.Add(wifi_Group.wifi_group_);

        TimeSpan mill_seconds_total = DateTime.Now.Subtract(dt_inizio);
        result.timestamp=(int)mill_seconds_total.TotalMilliseconds;
        result.device_id = chiave_orologio;

        string bodyJsonString = Newtonsoft.Json.JsonConvert.SerializeObject(result);
        //Debug.Log("bodyJsonString:" + bodyJsonString);

        //Debug.Log("url:" + uri);

        if (parla_con_server)
            StartCoroutine(PostData_Coroutine(uri, bodyJsonString, (value) => { Response_from_server(value); }));   

    }
    void chiama_ed_invia_calibrazione()
    {
        Debug.Log("Chiamo server calibrazione con attuali dati");

        string install_prefix = "70c0637bba8bd6168c03f8dd803ffecc";
        string site_domain = "kibitest.cloud";
        string edge_url = "https://" + install_prefix + "-loc." + site_domain + "/api";
        string area_id = calibrazione_class.area_id;
        string uri = edge_url + "/v1/send_data/" + area_id;

        int cont = 0;
        List<List<wifi_entry>> result = new List<List<wifi_entry>>();
        foreach (wifi_group w in calibrazione_class.list_of_wifi_group)
        {
            result.Add(w.wifi_group_);
            cont++;
        }

        Debug.Log("Sending " + cont.ToString() + " elements to calibration");
        total_calibration_wifi_sent = total_calibration_wifi_sent + cont;
        Debug.Log("Total element sent: " + total_calibration_wifi_sent.ToString());
        string bodyJsonString = Newtonsoft.Json.JsonConvert.SerializeObject(result);
        //Debug.Log("bodyJsonString:" + bodyJsonString);

        //Debug.Log("url:" + uri);

        if (parla_con_server)
            StartCoroutine(PostData_Coroutine(uri, bodyJsonString, (value) => { Response_from_server(value); }));



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


    public void Response_from_server(string return_string)
    {
        message_to_be_analyzed = true;
        JObject json = JObject.Parse(return_string);

        Debug.Log(json);
        /*foreach (var e in json)
        {
            Debug.Log(e);
        }*/

        // Debug.Log("Checking single values: paired:" + json["paired"]);


    }
    public void Response_from_get_area_list(string return_string)
    {

        msg_from_get_area = true;
        areas_list.areas = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Area>>(return_string);
        //areas_lists = new List<area_list_response>();

        /*
        foreach (area_list_response obj in objects)
        {
            string single_obj_string = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
            JObject json_obj = JObject.Parse(single_obj_string);

            Area_to_calibrate beacon_obj = new Area_to_calibrate(json_obj["area"].ToString(), 0, json_obj["id"].ToString());
            beacon_obj.distance = calcola_potenza_beacon(beacon_obj.name);

            areas_lists.Add(beacon_obj);

            beacon_obj.stampa_info();

            //aggiorna_disanze

        }*/

    }

    IEnumerator PostData_Coroutine(string url, string bodyJsonString, System.Action<string> callback)
    {
        var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

        string chiave_orologio = "b235d3df-7e33-4533-b750-febc8a8dd931";
        string auth_hand = "Basic " + chiave_orologio;
        //Debug.Log(auth_hand);
        request.SetRequestHeader("Authorization", auth_hand);
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ProtocolError || request.responseCode != 200)
        {
            Debug.Log("Status Code: " + request.responseCode);
            Debug.Log("Error: " + request.error);
        }
        callback(request.downloadHandler.text);

    }

    IEnumerator PostData_Coroutine_empty_body(string url, System.Action<string> callback)
    {
        var request = new UnityWebRequest(url, "POST");
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

        string chiave_orologio = "b235d3df-7e33-4533-b750-febc8a8dd931";
        string auth_hand = "Basic " + chiave_orologio;
        //Debug.Log(auth_hand);
        request.SetRequestHeader("Authorization", auth_hand);
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ProtocolError || request.responseCode != 200)
        {
            Debug.Log("Status Code: " + request.responseCode);
            Debug.Log("Error: " + request.error);
        }
        callback(request.downloadHandler.text);

    }
    IEnumerator GetData_Coroutine(string url, System.Action<string> callback)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            string chiave_orologio = "b235d3df-7e33-4533-b750-febc8a8dd931";
            string auth_hand = "Basic " + chiave_orologio;
            //Debug.Log(auth_hand);
            request.SetRequestHeader("Authorization", auth_hand);
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            Debug.Log(request.downloadHandler.text);
            //Debug.Log("Status Code: " + request.responseCode);
            if (request.result == UnityWebRequest.Result.ProtocolError || request.responseCode != 200)
            {
                Debug.Log("Status Code: " + request.responseCode);
                Debug.Log("Error: " + request.error);
            }
            callback(request.downloadHandler.text);
        }



    }



    public class alert_request
    {
        public string alert_id { get; set; }
        public string device_id { get; set; }
        public double timestamp { get; set; }
        public alert_request(string alert_id_, string device_id_, double timestamp_)
        {
            alert_id = alert_id_;
            device_id = device_id_;
            timestamp = timestamp_;

        }
    }
    public class orologio_request
    {
        public int battery { get; set; }
        public string version { get; set; }
        public orologio_request(int battery_, string version_)
        {
            battery = battery_;
            version = version_;

        }
    }
    public class orologio_response
    {
        public int paired { get; set; }
        public int parameter_update { get; set; }
        public int vibrator { get; set; }
        public orologio_response(int paired_, int parameter_update_, int vibrator_)
        {
            paired = paired_;
            parameter_update = parameter_update_;
            vibrator = vibrator_;
        }
    }
    public class Area
    {
        public string area { get; set; }
        public string type { get; set; }
        public int floor { get; set; }
        public int max_capacity { get; set; }
        public int last_scan_id { get; set; }
        public string id { get; set; }
        public int confidence { get; set; }
        public Area(string area_, string type_, int floor_, int max_capacity_, int last_scan_id_, string id_, int confidence_)
        {
            area = area_;
            type = type_;
            floor = floor_;
            max_capacity = max_capacity_;
            last_scan_id = last_scan_id_;
            id = id_;
            confidence = confidence_;

        }
    }
    public class Areas
    {
        public List<Area> areas;
        public Areas()
        {
            areas = new List<Area>();
        }
        public string cerca_id_tramite_nome(string nome)
        {
            foreach (Area area in areas)
            {
                if (area.area == nome)
                {
                    return area.id;
                }
            }
            return "null";
        }
    }
    /*public class Area_to_calibrate
    {
        public float distance { get; set; }
        public string name { get; set; }
        public string area_id { get; set; }
        public Area_to_calibrate()
        {
            name = "NO-NAME";
            distance = 0;

        }
        public Area_to_calibrate(string name_, float distance_,string area_id_)
        {
            name = name_;
            distance = distance_;
            area_id = area_id_;
        }
        public void stampa_info()
        {
            Debug.Log("Beacon:" + name + ";" + distance + ";" + area_id);

        }

    }*/
    public class Classe_calibrazione
    {
        public string area_id;
        public DateTime dt_inizio;
        public int index_distanze = 0;
        public List<wifi_group> list_of_wifi_group;
        //public float[] distanze_raccolte;
        public bool calibrazione_avviata = false;

        public Classe_calibrazione()
        {
        }
        public void stampa_info()
        {
            Debug.Log("id_stanza:" + area_id);
            Debug.Log("Distanze raccolte:" + index_distanze);
            string s = "";
            if (index_distanze > 0)
            {
                foreach (wifi_group group in list_of_wifi_group)
                {
                    s = s + "group:";
                    foreach (wifi_entry w in group.wifi_group_)
                    {
                        s = s + w.wifi_name + " " + w.wifi_level.ToString() + "/";

                    }
                    s = s + ";;;;;;;;";

                }
                Debug.Log("Wifi values:" + s);
            }
            else
            {
                Debug.Log("Wifi values vuoto");
            }
        }
        public void incorpora_nuovo_gruppo(wifi_group wifi_Group)
        {
            list_of_wifi_group.Add(wifi_Group);
            index_distanze++;

        }
        public void inizializza(string area_id_)
        {
            dt_inizio = DateTime.Now;
            list_of_wifi_group = new List<wifi_group>();
            calibrazione_avviata = true;
            index_distanze = 0;
            area_id = area_id_;

        }
    }
    public class wifi_entry
    {
        public string wifi_name { get; set; }
        public int wifi_level { get; set; }
        public wifi_entry()
        {

        }
    }
    public class wifi_group
    {
        public List<wifi_entry> wifi_group_;
        public wifi_group()
        {
            wifi_group_ = new List<wifi_entry>();
        }
    }

    public class Beacons
    {
        public List<SpriteRenderer> Beacons_list;
        public Beacons() {
            Beacons_list = new List<SpriteRenderer>();
        }
        public SpriteRenderer cerca_sprite_tramite_nome(string nome)
        {
            foreach (SpriteRenderer beacon in Beacons_list)
            {
                if (beacon.name == nome)
                {
                    return beacon;
                }
            }
            return null;
        }
    }

    public class Classe_localizzazione {
        public int timestamp;
        public string device_id;
        public List<List<wifi_entry>> data;
        public Classe_localizzazione(){
            data = new List<List<wifi_entry>>();
            
        }

    }
    public class Oggetto_id_area_for_server
    {
        public string id;
        public Oggetto_id_area_for_server()
        {

        }

    }
}
