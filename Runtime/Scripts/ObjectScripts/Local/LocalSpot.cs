using Newtonsoft.Json.Linq;
using UnityCEClient;
using UnityEngine;

public class LocalSpot : LocalObject
{
    public class ColorData
    {
        public float r, g, b, a;

        public ColorData() { }

        public ColorData(Color from)
        {
            this.r = from.r;
            this.g = from.g;
            this.b = from.b;
            this.a = from.a;
        }

        public void Update(Color from)
        {
            this.r = from.r;
            this.g = from.g;
            this.b = from.b;
            this.a = from.a;
        }
    }

    Light lightComponent;

    ColorData lightColor = new ColorData();
    float angle, intensity, range;

    protected override void Start()
    {
        base.Start();

        lightComponent = GetComponent<Light>();

        objectData.Add("color", GetColor);
        objectData.Add("angle", GetAngle);
        objectData.Add("intensity", GetIntensity);
        objectData.Add("range", GetRange);
    }

    protected override void Update()
    {
        base.Update();

        lightColor.Update(lightComponent.color);
        angle = lightComponent.spotAngle;
        range = lightComponent.range;
        intensity = lightComponent.intensity;
    }

    private (System.Type, object) GetColor()
    {
        return (typeof(ColorData), lightColor);
    }

    private (System.Type, object) GetIntensity()
    {
        return (typeof(float), intensity);
    }

    private (System.Type, object) GetAngle()
    {
        return (typeof(float), angle);
    }

    private (System.Type, object) GetRange()
    {
        return (typeof(float), range);
    }
}
