using System;
using MagicUI.Core;
using MagicUI.Styles;
using UnityEngine;

namespace MagicUI.Elements;

//
// Summary:
//     A container that overlays a single element on top of a background image. The
//     background will always be scaled to surround the element. To have a Sprite scale
//     without stretching its borders, use MagicUI.Graphics.TextureExtensions.ToSlicedSprite(UnityEngine.Texture2D,System.Single,System.Single,System.Single,System.Single)
//     to create the sprite.
[Stylable]
//[Obsolete("This is an extremely hacky workaround, it needs to be switched out the moment MagicUI implements a better solution")]
public sealed class ToggleButton : Container
{
    private Image backgroundObj;

    private float minWidth;

    private float minHeight;

    private Padding borders = Padding.Zero;

    //
    // Summary:
    //     The minimum width of the background
    public float MinWidth
    {
        get
        {
            return minWidth;
        }
        set
        {
            if (minWidth != value)
            {
                minWidth = value;
                InvalidateMeasure();
            }
        }
    }

    //
    // Summary:
    //     The minimum height of the background
    public float MinHeight
    {
        get
        {
            return minHeight;
        }
        set
        {
            if (minHeight != value)
            {
                minHeight = value;
                InvalidateMeasure();
            }
        }
    }

    //
    // Summary:
    //     How far around the enclosed element the background will stretch to (left, top,
    //     right, bottom)
    public Padding Borders
    {
        get
        {
            return borders;
        }
        set
        {
            if (borders != value)
            {
                borders = value;
                InvalidateMeasure();
            }
        }
    }

    private readonly string name = string.Empty;
    private readonly LayoutRoot layoutRoot;
    private readonly Sprite onSprite;
    private readonly Sprite offSprite;
    private readonly Action<Button> onClick;

    public bool IsOn { get; private set; } = false;
    //
    // Summary:
    //     Creates a panel
    //
    // Parameters:
    //   onLayout:
    //     The layout root to draw the panel on
    //
    //   background:
    //     The sprite of the background
    //
    //   name:
    //     The name of the panel
    public ToggleButton(LayoutRoot onLayout, Sprite onSprite, Sprite offSprite, Action<Button> onClick, string panelName = "New Panel")
        : base(onLayout, panelName)
    {
        layoutRoot = onLayout;
        name = panelName + " Background";
        this.onSprite = onSprite;
        this.offSprite = offSprite;
        this.onClick = onClick;
        SetImage(offSprite);
    }

    public void SetButton(Button button)
    {
        button.Click += Toggle;
        button.Click += onClick;
        Child = button;
    }

    public void SetImage(Sprite background)
    {
        if(backgroundObj != null)
        {
            DetachLogicalChild(backgroundObj);
        }
        backgroundObj?.Destroy();
        backgroundObj = new Image(layoutRoot, background, name);
        SetLogicalChild(backgroundObj);
        minWidth = background.rect.width;
        minHeight = background.rect.height;
        InvalidateArrange();
        InvalidateMeasure();
    }

    public void Toggle(Button sender)
    {
        IsOn = !IsOn;
        if(IsOn)
        {
            SetImage(onSprite);
        }
        else
        {
            SetImage(offSprite);
        }
        ReconstructButton();
    }

    private void ReconstructButton()
    {
        Button newButton = new(layoutRoot, Child.Name)
        {
            Borderless = ((Button)Child).Borderless,
            Content = ((Button)Child).Content,
            FontSize = ((Button)Child).FontSize,
            Margin = ((Button)Child).Margin,
            MinWidth = ((Button)Child).MinWidth,
            MinHeight = ((Button)Child).MinHeight,
            BorderColor = ((Button)Child).BorderColor,
            ContentColor = ((Button)Child).ContentColor,
            Font = ((Button)Child).Font,
            Enabled = ((Button)Child).Enabled,
        };
        // THIS ORDER IS IMPORTANT, the value here needs to be updated first because the onClick might read from it
        newButton.Click += Toggle;
        newButton.Click += onClick;
        Child.Destroy();
        Child = newButton;
        InvalidateArrange();
        InvalidateMeasure();
    }

    protected override Vector2 MeasureOverride()
    {
        base.Child?.Measure();
        if (base.Child != null)
        {
            backgroundObj.Width = Math.Max(MinWidth, base.Child.EffectiveSize.x + borders.AddedWidth);
            backgroundObj.Height = Math.Max(MinHeight, base.Child.EffectiveSize.y + borders.AddedHeight);
        }
        else
        {
            backgroundObj.Width = MinWidth;
            backgroundObj.Height = MinHeight;
        }

        return backgroundObj.Measure();
    }

    protected override void ArrangeOverride(Vector2 alignedTopLeftCorner)
    {
        base.Child?.Arrange(new Rect(alignedTopLeftCorner + new Vector2(borders.Left, borders.Top), new Vector2(base.ContentSize.x - borders.AddedWidth, base.ContentSize.y - borders.AddedHeight)));
        backgroundObj.Arrange(new Rect(alignedTopLeftCorner, base.ContentSize));
    }

    protected override void DestroyOverride()
    {
        base.Child?.Destroy();
        backgroundObj.Destroy();
    }
}