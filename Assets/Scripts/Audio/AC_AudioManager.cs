using UnityEngine;

/// <summary>
/// Puntos de inserción de audio. Todos los métodos son null-safe:
/// si no hay AudioClip asignado, no hace nada.
/// Para usar: arrastrar clips .wav/.mp3 a los campos en el Inspector.
/// </summary>
public class AC_AudioManager : MonoBehaviour
{
    [Header("Música")]
    public AudioClip musicaMenu;
    public AudioClip musicaPartida;

    [Header("Efectos")]
    public AudioClip sonidoAbrazo;
    public AudioClip sonidoAbrazoFallido;
    public AudioClip sonidoCaida;
    public AudioClip sonidoCuentaAtras;
    public AudioClip sonidoInicioRonda;
    public AudioClip sonidoFinRonda;
    public AudioClip sonidoFinPartida;
    public AudioClip sonidoDash;

    private AudioSource fuenteMusica;
    private AudioSource fuenteEfectos;

    private void Awake()
    {
        fuenteMusica = gameObject.AddComponent<AudioSource>();
        fuenteMusica.loop = true;
        fuenteMusica.playOnAwake = false;
        fuenteMusica.volume = 0.5f;

        fuenteEfectos = gameObject.AddComponent<AudioSource>();
        fuenteEfectos.loop = false;
        fuenteEfectos.playOnAwake = false;
    }

    public void ReproducirMusicaMenu()
    {
        if (musicaMenu == null) return;
        fuenteMusica.clip = musicaMenu;
        fuenteMusica.Play();
    }

    public void ReproducirMusicaPartida()
    {
        if (musicaPartida == null) return;
        fuenteMusica.clip = musicaPartida;
        fuenteMusica.Play();
    }

    public void DetenerMusica()
    {
        fuenteMusica.Stop();
    }

    public void ReproducirAbrazo()
    {
        if (sonidoAbrazo != null) fuenteEfectos.PlayOneShot(sonidoAbrazo);
    }

    public void ReproducirAbrazoFallido()
    {
        if (sonidoAbrazoFallido != null) fuenteEfectos.PlayOneShot(sonidoAbrazoFallido);
    }

    public void ReproducirCaida()
    {
        if (sonidoCaida != null) fuenteEfectos.PlayOneShot(sonidoCaida);
    }

    public void ReproducirCuentaAtras()
    {
        if (sonidoCuentaAtras != null) fuenteEfectos.PlayOneShot(sonidoCuentaAtras);
    }

    public void ReproducirInicioRonda()
    {
        if (sonidoInicioRonda != null) fuenteEfectos.PlayOneShot(sonidoInicioRonda);
    }

    public void ReproducirFinPartida()
    {
        if (sonidoFinPartida != null) fuenteEfectos.PlayOneShot(sonidoFinPartida);
    }

    public void ReproducirDash()
    {
        if (sonidoDash != null) fuenteEfectos.PlayOneShot(sonidoDash);
    }
}
