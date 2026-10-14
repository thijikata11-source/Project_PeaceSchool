using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

public class CutoffMaskUI : Image
{
    public override Material materialForRendering
    {
        get
        {
            // Create a new material instance based on the default rendering material
            Material material = new Material(base.materialForRendering);

            // Change the stencil comparison function to "NotEqual"
            material.SetInt("_StencilComp", (int)CompareFunction.NotEqual);

            return material;
        }
    }
}
