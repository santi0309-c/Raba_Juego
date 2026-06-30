using UnityEngine;
using System.Collections.Generic;

public class AC_TatamiGenerator : MonoBehaviour
{
    public float anchoEstera = 0.9f;
    public float largoEstera = 1.8f;
    public float grosorEstera = 0.04f;
    public float grosorBorde = 0.03f;

    public Color colorPaja = new Color(0.77f, 0.70f, 0.35f, 1f);
    public Color colorBorde = new Color(0.08f, 0.12f, 0.25f, 1f);
    public Color colorPajaOscura = new Color(0.68f, 0.62f, 0.30f, 1f);

    public float radioArena = 7.5f;
    public float alturaPiso = 0f;

    public Material materialPaja;
    public Material materialBorde;

    private Transform contenedor;
    private List<GameObject> esterasGeneradas = new List<GameObject>();
    private int contadorEsteras;

    public void GenerarTatami()
    {
        LimpiarEsteras();
        contenedor = new GameObject("TatamiMats").transform;
        contenedor.SetParent(transform);
        contenedor.localPosition = Vector3.zero;
        contenedor.localRotation = Quaternion.identity;
        contadorEsteras = 0;

        GenerarGrillaEstiloJapones();
    }

    private void GenerarGrillaEstiloJapones()
    {
        float minCoord = -(radioArena - 1f);
        float maxCoord = radioArena - 1f;
        bool filaPar = true;

        for (float z = minCoord; z <= maxCoord; z += largoEstera * 0.5f)
        {
            float offsetX = filaPar ? 0f : anchoEstera * 0.5f;

            for (float x = minCoord + offsetX; x <= maxCoord; x += anchoEstera)
            {
                if (!EsteraDentroDeArena(x, z, anchoEstera * 0.5f, largoEstera * 0.5f))
                {
                    continue;
                }

                Vector3 posicion = new Vector3(x, alturaPiso + grosorEstera * 0.5f, z);

                float ruido = Mathf.PerlinNoise(x * 0.7f + 100f, z * 0.7f + 100f) * 0.35f;
                Color colorEstera = Color.Lerp(colorPaja, colorPajaOscura, ruido);

                CrearEstera(posicion, colorEstera);
                contadorEsteras++;
            }

            filaPar = !filaPar;
        }

        Debug.Log("[AC_TatamiGenerator] " + contadorEsteras + " esteras generadas en patron tradicional.");
    }

    private bool EsteraDentroDeArena(float cx, float cz, float mitadAncho, float mitadLargo)
    {
        Vector2[] puntos = new Vector2[5];
        puntos[0] = new Vector2(cx, cz);
        puntos[1] = new Vector2(cx + mitadAncho, cz + mitadLargo);
        puntos[2] = new Vector2(cx - mitadAncho, cz + mitadLargo);
        puntos[3] = new Vector2(cx + mitadAncho, cz - mitadLargo);
        puntos[4] = new Vector2(cx - mitadAncho, cz - mitadLargo);

        int dentro = 0;
        for (int i = 0; i < puntos.Length; i++)
        {
            if (puntos[i].magnitude <= radioArena - 0.3f)
            {
                dentro++;
            }
        }

        return dentro >= 3;
    }

    private void CrearEstera(Vector3 posicion, Color colorPajaEstera)
    {
        GameObject baseEstera = CrearPrimitiva(PrimitiveType.Cube,
            "Estera_" + contadorEsteras,
            posicion,
            new Vector3(anchoEstera * 0.92f, grosorEstera, largoEstera * 0.92f),
            materialPaja, colorPajaEstera);

        CrearBordesEstera(posicion, anchoEstera, largoEstera, grosorEstera, grosorBorde);
    }

    private void CrearBordesEstera(Vector3 centro, float ancho, float largo, float alto, float bordeAncho)
    {
        float yBorde = centro.y - alto * 0.5f + grosorEstera;
        float ruido = Mathf.PerlinNoise(centro.x * 1.3f, centro.z * 1.3f);
        Color bordeColor = Color.Lerp(colorBorde, colorBorde * 1.15f, ruido);

        CrearPrimitiva(PrimitiveType.Cube,
            "Borde_" + contadorEsteras + "X+",
            new Vector3(centro.x + ancho * 0.46f, yBorde, centro.z),
            new Vector3(bordeAncho, grosorEstera * 1.15f, largo * 0.92f),
            materialBorde, bordeColor);

        CrearPrimitiva(PrimitiveType.Cube,
            "Borde_" + contadorEsteras + "X-",
            new Vector3(centro.x - ancho * 0.46f, yBorde, centro.z),
            new Vector3(bordeAncho, grosorEstera * 1.15f, largo * 0.92f),
            materialBorde, bordeColor);

        CrearPrimitiva(PrimitiveType.Cube,
            "Borde_" + contadorEsteras + "Z+",
            new Vector3(centro.x, yBorde, centro.z + largo * 0.46f),
            new Vector3(ancho * 0.92f, grosorEstera * 1.15f, bordeAncho),
            materialBorde, bordeColor);

        CrearPrimitiva(PrimitiveType.Cube,
            "Borde_" + contadorEsteras + "Z-",
            new Vector3(centro.x, yBorde, centro.z - largo * 0.46f),
            new Vector3(ancho * 0.92f, grosorEstera * 1.15f, bordeAncho),
            materialBorde, bordeColor);
    }

    private GameObject CrearPrimitiva(PrimitiveType tipo, string nombre,
        Vector3 posicionLocal, Vector3 escala, Material material, Color color)
    {
        GameObject obj = GameObject.CreatePrimitive(tipo);
        obj.name = nombre;
        obj.transform.SetParent(contenedor);
        obj.transform.localPosition = posicionLocal;
        obj.transform.localScale = escala;
        obj.transform.localRotation = Quaternion.identity;

        Collider col = obj.GetComponent<Collider>();
        if (col != null)
        {
            if (Application.isPlaying)
            {
                Destroy(col);
            }
            else
            {
                DestroyImmediate(col);
            }
        }

        Renderer r = obj.GetComponent<Renderer>();
        if (r == null) return obj;

        if (material != null)
        {
            r.sharedMaterial = material;
            return obj;
        }

        r.material.color = color;

        return obj;
    }

    public void LimpiarEsteras()
    {
        Transform existente = transform.Find("TatamiMats");
        if (existente != null)
        {
            if (Application.isPlaying)
            {
                Destroy(existente.gameObject);
            }
            else
            {
                DestroyImmediate(existente.gameObject);
            }
        }
    }
}
