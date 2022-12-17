using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;
using System;
using System.Collections.Generic;

///@author Miia Arkko
///@version 4.2.2021
/// <summary>
/// Ohjelma, jossa kerätään pipareita
/// </summary>
public class JussiPeli : PhysicsGame
{
    private const double NOPEUS = 10000;
    private const double HYPPYNOPEUS = 750;
    private const int RUUDUN_KOKO = 50;

    private PlatformCharacter pelaaja1;

    private Image pelaajanKuva = LoadImage("jussiNaama.png");
    Image[] pelaajanHyppykuvat = LoadImages("jussiHyppy.png", "jussiNaama.png");
    private Image pelaajanKuolemakuva = LoadImage("jussiKuoli.png");
    private Image tahtiKuva = LoadImage("pipari.png");
    private Image vihollisenKuva = LoadImage("vihollinen2.png");
    private Image herraHankeynKuva = LoadImage("herraHankey.png");

    private SoundEffect syontiAani = LoadSoundEffect("jussiNomnomAani.wav");
    private SoundEffect kuolemaAani = LoadSoundEffect("jussiKuolemaAani.wav");
    private SoundEffect voittoAani = LoadSoundEffect("jihuuAani.wav");

    private bool peliKaynnissa = false;

    private Timer liikutusajastin;

    IntMeter pelaajanPisteet;


    /// <summary>
    /// Pääohjelma, jossa aliohjelmakutsut
    /// </summary>
    public override void Begin()
    {
        SetWindowSize(1920, 1080);

        Gravity = new Vector(0, -1000);

        LuoKentta();
        LisaaNappaimet();
        LisaaLaskuri();

        Camera.Follow(pelaaja1);
        Camera.FollowOffset = new Vector(Screen.Width / 2.5 - RUUDUN_KOKO, 0.0);
        Camera.ZoomFactor = 1.2;
        Camera.StayInLevel = true;

        liikutusajastin = new Timer();
        liikutusajastin.Interval = 0.01;
        liikutusajastin.Timeout += SiirraPelaajaaOikeammalle;
        liikutusajastin.Start();

        peliKaynnissa = true;
        //System.Threading.Thread.Sleep(5000);
    }


    /// <summary>
    /// Pistelaskuri, joka laskee pelaajan pisteet
    /// </summary>
    void LisaaLaskuri()
    {
        pelaajanPisteet = LuoPisteLaskuri(Screen.Left + 50, Screen.Top - 130);
    }


    /// <summary>
    /// Pistenäyttö peliruutuun
    /// </summary>
    /// <param name="x">Näytön leveys</param>
    /// <param name="y">Näytön korkeus</param>
    /// <returns>Laskuri</returns>
    IntMeter LuoPisteLaskuri(double x, double y)
    {
        IntMeter laskuri = new IntMeter(0);

        Label naytto = new Label(60, 60);
        naytto.BindTo(laskuri);
        naytto.X = x;
        naytto.Y = y;
        naytto.TextColor = Color.Black;
        naytto.BorderColor = Level.Background.Color;
        naytto.Color = Color.HotPink;
        //naytto.TextScale(2);
        Add(naytto);
        return laskuri;
    }

    /// <summary>
    /// Siirtää kuvaa oikealle
    /// </summary>
    void SiirraPelaajaaOikeammalle()
    {
        pelaaja1.Push(new Vector(NOPEUS, 0.0));
    }


    /// <summary>
    /// Luo pelikentän
    /// </summary>
    private void LuoKentta()
    {
        TileMap kentta = TileMap.FromLevelAsset("kentta1.txt");
        kentta.SetTileMethod('#', LisaaTaso);
        kentta.SetTileMethod('*', LisaaPipari);
        kentta.SetTileMethod('J', LisaaPelaaja);
        kentta.SetTileMethod('V', LisaaVihollinen);
        kentta.SetTileMethod('H', LisaaHerraHankey);
        kentta.Execute(RUUDUN_KOKO, RUUDUN_KOKO);
        //Level.CreateBorders();

        Level.CreateLeftBorder();
        Level.CreateTopBorder();
        Level.CreateBottomBorder();
        PhysicsObject oikeaReuna = Level.CreateRightBorder();
        oikeaReuna.Tag = "oikea";
        Level.Background.CreateGradient(Color.White, Color.SkyBlue);
    }

    /// <summary>
    /// Luo kentän tason
    /// </summary>
    /// <param name="paikka"></param>
    /// <param name="leveys">Tason leveys</param>
    /// <param name="korkeus">Tason korkeus</param>
    private void LisaaTaso(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject taso = PhysicsObject.CreateStaticObject(leveys, korkeus);
        taso.Position = paikka;
        taso.Color = Color.Black;
        taso.Tag = "seina";
        Add(taso);
    }

    /// <summary>
    /// Lisää yhden piparin
    /// </summary>
    /// <param name="paikka">Piparin paikka</param>
    /// <param name="leveys">Piparin leveys</param>
    /// <param name="korkeus">Piparin korkeus</param>
    private void LisaaPipari(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject pipari = PhysicsObject.CreateStaticObject(leveys, korkeus);
        pipari.IgnoresCollisionResponse = true;
        pipari.Position = paikka;
        pipari.Image = tahtiKuva;
        pipari.Tag = "pipari";
        Add(pipari);
    }


    /// <summary>
    /// Pelaajan määrittely
    /// </summary>
    /// <param name="paikka">Pelaajan alkusijainti kentässä</param>
    /// <param name="leveys">Pelaajan leveys</param>
    /// <param name="korkeus">Pelaajan korkeus</param>
    private void LisaaPelaaja(Vector paikka, double leveys, double korkeus)
    {
        pelaaja1 = new PlatformCharacter(leveys, korkeus);
        pelaaja1.Position = paikka;
        pelaaja1.Mass = 4.0;
        pelaaja1.Image = pelaajanKuva;
        pelaaja1.AnimJump = new Animation(pelaajanHyppykuvat);
        pelaaja1.AnimFall = new Animation(pelaajanKuva);
        AddCollisionHandler(pelaaja1, "pipari", SaaPipari);
        AddCollisionHandler(pelaaja1, "seina", TormaaKuolettavaan);
        AddCollisionHandler(pelaaja1, "vihu", TormaaKuolettavaan);
        AddCollisionHandler(pelaaja1, "oikea", TormaaOikeaanReunaan);
        AddCollisionHandler(pelaaja1, "hankey", TormaaKuolettavaan);
        
        Add(pelaaja1);
    }


    /// <summary>
    /// Mitä tapahtuu, kun päästään pelin oikeaan reunaan
    /// </summary>
    /// <param name="tormaaja">Pelaaja, joka törmää</param>
    /// <param name="kohde">Oikea reuna, johon törmätään</param>
    void TormaaOikeaanReunaan(PhysicsObject tormaaja, PhysicsObject kohde)
    {
        Gravity = Vector.Zero;
        StopAll();
        Keyboard.Disable(Key.Up);
        MessageDisplay.Add("Tulit maaliin!");
        voittoAani.Play();

    }

    /// <summary>
    /// Peliin lisättävä vihollinen
    /// </summary>
    /// <param name="paikka">Alkupaikka kentässä</param>
    /// <param name="leveys">Vihollisen leveys</param>
    /// <param name="korkeus">Vihollisen korkeus</param>
    private void LisaaVihollinen(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject vihollinen = new PhysicsObject(leveys, korkeus);
        vihollinen.Position = paikka;
        vihollinen.CanRotate = false;
        vihollinen.IgnoresCollisionResponse = true;
        vihollinen.Oscillate(new Vector(0, 1), korkeus * 1.5, 0.3);
        vihollinen.Image = vihollisenKuva;
        vihollinen.Tag = "vihu";
        Add(vihollinen);
    }

    /// <summary>
    /// Peliin lisättävä vihollinen HerraHankey
    /// </summary>
    /// <param name="paikka">Alkupaikka kentällä</param>
    /// <param name="leveys">HerraHankeyn leveys</param>
    /// <param name="korkeus">HerraHankeys korkeus</param>
    private void LisaaHerraHankey(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject herraHankey = new PhysicsObject(leveys, korkeus);
        herraHankey.Position = paikka;
        herraHankey.CanRotate = false;
        herraHankey.IgnoresCollisionResponse = true;
        herraHankey.Oscillate(new Vector(0, 1), korkeus * 3, 0.5);
        herraHankey.Image = herraHankeynKuva;
        herraHankey.Tag = "hankey";
        Add(herraHankey);
    }


    /// <summary>
    /// Mitä tapahtuu, kun pelaaja törmää johonkin kuolettavaan
    /// </summary>
    /// <param name="tormaaja">Pelaaja, joka törmää</param>
    /// <param name="kohde">Kkuolettava kohde, johon pelaaja törmää</param>
    void TormaaKuolettavaan(PhysicsObject tormaaja, PhysicsObject kohde)
    {
        if (peliKaynnissa)
        {
            pelaaja1.Image = pelaajanKuolemakuva;
            Animation kuolemisanimaatio = new Animation(pelaajanKuolemakuva);
            pelaaja1.AnimJump = kuolemisanimaatio;
            pelaaja1.AnimFall = kuolemisanimaatio;
            MessageDisplay.Add("Kuolit! :(");
            Keyboard.Disable(Key.Up);
            liikutusajastin.Stop();
            peliKaynnissa = false;
            kuolemaAani.Play();
        }
    }


    /// <summary>
    /// Lisää ohjaimet peliin
    /// </summary>
    private void LisaaNappaimet()
    {
        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Näytä ohjeet");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");

        Keyboard.Listen(Key.Up, ButtonState.Pressed, Hyppaa, "Pelaaja hyppää", pelaaja1, HYPPYNOPEUS);

        ControllerOne.Listen(Button.Back, ButtonState.Pressed, Exit, "Poistu pelistä");
        //ControllerOne.Listen(Button.A, ButtonState.Pressed, Hyppaa, "Pelaaja hyppää", pelaaja1, HYPPYNOPEUS);

        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
    }


    /// <summary>
    /// Pakotetaan hahmo hyppäämään ilmassa
    /// </summary>
    /// <param name="hahmo">Pelaaja</param>
    /// <param name="nopeus">Nopeus</param>
    private void Hyppaa(PlatformCharacter hahmo, double nopeus)
    {
        hahmo.ForceJump(nopeus);
    }


    /// <summary>
    /// Tapahtumat, kun törmätään pipariin
    /// </summary>
    /// <param name="hahmo"></param>
    /// <param name="pipari"></param>
    private void SaaPipari(PhysicsObject hahmo, PhysicsObject pipari)
    {
        
        pelaajanPisteet.Value += 1;
        syontiAani.Play();
        MessageDisplay.Add("Sait piparia!");
        pipari.Destroy();
    }
}

