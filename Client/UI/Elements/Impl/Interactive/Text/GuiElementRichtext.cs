﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client
{
    public class GuiElementRichtext : GuiElement
    {
        public RichTextComponentBase[] Components;
        protected TextFlowPath[] flowPath;
        public float zPos = 50;

        public static bool DebugLogging = false;

        public LoadedTexture richtTextTexture;

        public bool Debug = false;

        public Vec4f RenderColor;
        

        public double MaxLineWidth
        {
            get
            {
                if (flowPath == null) return 0;

                double x = 0;
                for (int i = 0; i < Components.Length; i++)
                {
                    var cmp = Components[i];
                    for (int j = 0; j < cmp.BoundsPerLine.Length; j++)
                    {
                        x = Math.Max(x, cmp.BoundsPerLine[j].X + cmp.BoundsPerLine[j].Width);
                    }
                }

                return x;
            }
        }

        public double TotalHeight
        {
            get
            {
                if (flowPath == null) return 0;

                double y = 0;
                for (int i = 0; i < Components.Length; i++)
                {
                    var cmp = Components[i];
                    for (int j = 0; j < cmp.BoundsPerLine.Length; j++)
                    {
                        y = Math.Max(y, cmp.BoundsPerLine[j].Y + cmp.BoundsPerLine[j].Height);
                    }
                }

                return y;
            }
        }

        public GuiElementRichtext(ICoreClientAPI capi, RichTextComponentBase[] components, ElementBounds bounds) : base(capi, bounds)
        {
            this.Components = components;

            richtTextTexture = new LoadedTexture(capi);
        }

        public override void BeforeCalcBounds()
        {
            CalcHeightAndPositions();
        }


        public override void ComposeElements(Context ctxStatic, ImageSurface surfaceStatic)
        {
            Compose();
        }


        ImageSurface surface;
        Context ctx;

        public bool HalfComposed;

        public void Compose(bool genTextureLater = false)
        {
            ElementBounds rtbounds = Bounds.CopyOnlySize();
            rtbounds.fixedPaddingX = 0;
            rtbounds.fixedPaddingY = 0;


            Bounds.CalcWorldBounds();

            int wdt = (int)Bounds.InnerWidth;
            int hgt = (int)Bounds.InnerHeight;
            if (richtTextTexture.TextureId != 0)
            {
                wdt = Math.Max(1, Math.Max(wdt, richtTextTexture.Width));
                hgt = Math.Max(1, Math.Max(hgt, richtTextTexture.Height));
            }

            surface = new ImageSurface(Format.ARGB32, wdt, hgt);
            ctx = new Context(surface);
            ctx.SetSourceRGBA(0, 0, 0, 0);
            ctx.Paint();

            if (!genTextureLater)
            {
                ComposeFor(rtbounds, ctx, surface);
                generateTexture(surface, ref richtTextTexture);

                ctx.Dispose();
                surface.Dispose();
                ctx = null;
                surface = null;
            } else
            {
                HalfComposed = true;
            }
        }


        public void genTexture()
        {
            generateTexture(surface, ref richtTextTexture);

            ctx.Dispose();
            surface.Dispose();
            ctx = null;
            surface = null;
            HalfComposed = false;
        }

        public void CalcHeightAndPositions()
        {
            Bounds.CalcWorldBounds();

            if (DebugLogging)
            {
                api.Logger.VerboseDebug("GuiElementRichtext: before bounds: {0}/{1}  w/h = {2},{3}", Bounds.absX, Bounds.absY, Bounds.OuterWidth, Bounds.OuterHeight);
            }

            double posX = 0;
            double posY = 0;

            List<int> currentLine = new List<int>();
            List<TextFlowPath> flowPathList = new List<TextFlowPath>();

            flowPathList.Add(new TextFlowPath(Bounds.InnerWidth));

            double lineHeight = 0;
            double ascentHeight = 0;
            RichTextComponentBase comp = null;

            for (int i = 0; i < Components.Length; i++)
            {
                comp = Components[i];

                bool didLineBreak = comp.CalcBounds(flowPathList.ToArray(), lineHeight, posX, posY);

                if (DebugLogging)
                {
                    api.Logger.VerboseDebug("GuiElementRichtext, add comp {0}, posY={1}, lineHeight={2}", i, posY, lineHeight);
                    api.Logger.VerboseDebug("GuiElementRichtext, Comp bounds 0 w/h: {0}/{1}", comp.BoundsPerLine[0].Width, comp.BoundsPerLine[0].Height);
                }


                posX += comp.PaddingLeft;

                if (comp.Float == EnumFloat.None)
                {
                    posX = 0;
                    posY += Math.Max(lineHeight, comp.BoundsPerLine[0].Height) + comp.MarginTop;
                    posY = Math.Ceiling(posY);
                    
                    currentLine.Clear();
                    lineHeight = 0;
                    ascentHeight = 0;
                    continue;
                }

                if (didLineBreak)
                {
                    lineHeight = Math.Ceiling(Math.Max(lineHeight, comp.BoundsPerLine[0].Height));
                    ascentHeight = Math.Ceiling(Math.Max(ascentHeight, comp.BoundsPerLine[0].AscentOrHeight));

                    // All previous elements in this line might need to have their Y pos adjusted due to a larger element in the line
                    foreach (int index in currentLine)
                    {
                        RichTextComponentBase lineComp = Components[index];
                        Rectangled lastLineBounds = lineComp.BoundsPerLine[lineComp.BoundsPerLine.Length - 1];
                        
                        if (lineComp.VerticalAlign == EnumVerticalAlign.Bottom)
                        {
                            lastLineBounds.Y = Math.Ceiling(lastLineBounds.Y + ascentHeight - lineComp.BoundsPerLine[lineComp.BoundsPerLine.Length - 1].AscentOrHeight);
                        }
                        if (lineComp.VerticalAlign == EnumVerticalAlign.Middle)
                        {
                            lastLineBounds.Y = Math.Ceiling(lastLineBounds.Y + ascentHeight - lineComp.BoundsPerLine[lineComp.BoundsPerLine.Length - 1].AscentOrHeight / 2);
                        }
                    }

                    // The current element that was still on the same line as well
                    // Offset all lines by the gained y-offset on the first line
                    if (comp.VerticalAlign == EnumVerticalAlign.Bottom)
                    {
                        foreach (var val in comp.BoundsPerLine) val.Y = Math.Ceiling(val.Y + ascentHeight - comp.BoundsPerLine[0].AscentOrHeight);
                    }
                    if (comp.VerticalAlign == EnumVerticalAlign.Middle)
                    {
                        foreach (var val in comp.BoundsPerLine) val.Y = Math.Ceiling(val.Y + ascentHeight - comp.BoundsPerLine[0].AscentOrHeight / 2);
                    }
                    
                    currentLine.Clear();
                    currentLine.Add(i);

                    
                    posY += lineHeight;
                    for (int k = 1; k < comp.BoundsPerLine.Length - 1; k++) posY += comp.BoundsPerLine[k].Height;
                    posY = Math.Ceiling(posY);


                    posX = comp.BoundsPerLine[comp.BoundsPerLine.Length - 1].Width;
                    if (comp.BoundsPerLine[comp.BoundsPerLine.Length - 1].Width > 0)
                    {
                        lineHeight = comp.BoundsPerLine[comp.BoundsPerLine.Length - 1].Height;
                        ascentHeight = comp.BoundsPerLine[comp.BoundsPerLine.Length - 1].AscentOrHeight;
                    } else
                    {
                        lineHeight = 0;
                        ascentHeight = 0;
                    }

                } else
                {
                    if (comp.Float == EnumFloat.Inline && comp.BoundsPerLine.Length > 0)
                    {
                        posX += comp.BoundsPerLine[0].Width;
                        lineHeight = Math.Max(comp.BoundsPerLine[0].Height, lineHeight);
                        ascentHeight = Math.Max(comp.BoundsPerLine[0].AscentOrHeight, ascentHeight);                        
                        currentLine.Add(i);
                    }
                }

                if (comp.Float != EnumFloat.Inline)
                {
                    ConstrainTextFlowPath(flowPathList, posY, comp.BoundsPerLine[0], comp.Float);
                }
            }


            if (DebugLogging)
            {
                api.Logger.VerboseDebug("GuiElementRichtext: after loop. posY = {0}", posY);
            }


            if (comp != null && posX > 0 && comp.BoundsPerLine.Length > 0)
            {
               posY += lineHeight;
            }

            //if (Components.Length > 0) - what is this for?
            {
                Bounds.fixedHeight = (posY + 1) / RuntimeEnv.GUIScale;
            }
            

            foreach (int index in currentLine)
            {
                RichTextComponentBase lineComp = Components[index];
                Rectangled lastLineBounds = lineComp.BoundsPerLine[lineComp.BoundsPerLine.Length - 1];

                if (lineComp.VerticalAlign == EnumVerticalAlign.Bottom)
                {
                    lastLineBounds.Y = Math.Ceiling(lastLineBounds.Y + ascentHeight - lineComp.BoundsPerLine[lineComp.BoundsPerLine.Length - 1].AscentOrHeight);
                }
                if (lineComp.VerticalAlign == EnumVerticalAlign.Middle)
                {
                    lastLineBounds.Y = Math.Ceiling(lastLineBounds.Y + ascentHeight - lineComp.BoundsPerLine[lineComp.BoundsPerLine.Length - 1].AscentOrHeight / 2);
                }
            }

            this.flowPath = flowPathList.ToArray();

            if (DebugLogging)
            {
                api.Logger.VerboseDebug("GuiElementRichtext: after bounds: {0}/{1}  w/h = {2},{3}", Bounds.absX, Bounds.absY, Bounds.OuterWidth, Bounds.OuterHeight);
                api.Logger.VerboseDebug("GuiElementRichtext: posY = {0}", posY);

                api.Logger.VerboseDebug("GuiElementRichtext: framewidth/height: {0}/{1}", api.Render.FrameWidth, api.Render.FrameHeight);
            }
        }



        private double getLeftIndentAt(List<TextFlowPath> flowPath, double posY)
        {
            for (int i = 0; i < flowPath.Count; i++)
            {
                TextFlowPath tfp = flowPath[i];
                if (tfp.Y1 <= posY && tfp.Y2 >= posY) return tfp.X1;
            }

            return 0;
        }


        private void ConstrainTextFlowPath(List<TextFlowPath> flowPath, double posY, Rectangled rect, EnumFloat elementFloat)
        {
            double x1 = elementFloat == EnumFloat.Left ? rect.Width : 0;
            double x2 = elementFloat == EnumFloat.Right ? Bounds.InnerWidth - rect.Width : Bounds.InnerWidth;

            double remainingHeight = rect.Height;

            for (int i = 0; i < flowPath.Count; i++)
            {
                TextFlowPath tfp = flowPath[i];

                if (tfp.Y2 <= posY) continue; // we already passed this one

                double hereX1 = Math.Max(x1, tfp.X1);
                double hereX2 = Math.Min(x2, tfp.X2);


                // Current bounds are taller, let's make a split and insert ours
                if (tfp.Y2 > posY + rect.Height)
                {
                    // Already more contrained, don't touch
                    if (x1 <= tfp.X1 && x2 >= tfp.X2) continue;

                    if (i == 0)
                    {
                        // We're at the begining, so don't need a "before" element
                        flowPath[i] = new TextFlowPath(hereX1, posY, hereX2, posY + rect.Height);
                        flowPath.Insert(i + 1, new TextFlowPath(tfp.X1, posY + rect.Height, tfp.X2, tfp.Y2));
                    } else
                    {
                        flowPath[i] = new TextFlowPath(tfp.X1, tfp.Y1, tfp.X2, posY);
                        flowPath.Insert(i + 1, new TextFlowPath(tfp.X1, posY + rect.Height, tfp.X2, tfp.Y2));
                        flowPath.Insert(i, new TextFlowPath(hereX1, posY, hereX2, posY + rect.Height));
                    }
                    
                    remainingHeight = 0;
                    break;
                } else

                // Current bounds are shorter, let's update it
                {
                    flowPath[i].X1 = hereX1;
                    flowPath[i].X2 = hereX2;
                    remainingHeight -= tfp.Y2 - posY;
                }
            }

            if (remainingHeight > 0)
            {
                flowPath.Add(new TextFlowPath(x1, posY, x2, posY + remainingHeight));
            }
        }



        public virtual void ComposeFor(ElementBounds bounds, Context ctx, ImageSurface surface)
        {
            bounds.CalcWorldBounds();

            ctx.Save();
            Matrix m = ctx.Matrix;
            m.Translate(bounds.drawX, bounds.drawY);
            ctx.Matrix = m;

            for (int i = 0; i < Components.Length; i++)
            { 
                Components[i].ComposeElements(ctx, surface);

                if (Debug)
                {
                    ctx.LineWidth = 1f;
                    if (Components[i] is ClearFloatTextComponent)
                    {
                        ctx.SetSourceRGBA(0, 0, 1, 0.5);
                    } else
                    {
                        ctx.SetSourceRGBA(0, 0, 0, 0.5);
                    }

                    foreach (var val in Components[i].BoundsPerLine)
                    {
                        ctx.Rectangle(val.X, val.Y, val.Width, val.Height);
                        ctx.Stroke();
                    }
                } 
            }

            ctx.Restore();
        }


        public override void RenderInteractiveElements(float deltaTime)
        {
            Render2DTexture(
                richtTextTexture.TextureId, 
                (int)Bounds.renderX, 
                (int)Bounds.renderY, 
                (int)richtTextTexture.Width,
                (int)richtTextTexture.Height,
                zPos,
                RenderColor
            );

            bool found = false;
            int relx = (int)(api.Input.MouseX - Bounds.absX);
            int rely = (int)(api.Input.MouseY - Bounds.absY);

            MouseOverCursor = null;
            for (int i = 0; i < Components.Length; i++) {
                RichTextComponentBase comp = Components[i];

                comp.RenderColor = RenderColor;
                comp.RenderInteractiveElements(deltaTime, Bounds.renderX, Bounds.renderY);

                for (int j = 0; !found && j < comp.BoundsPerLine.Length; j++)
                {
                    LineRectangled rec = comp.BoundsPerLine[j];

                    if (rec.PointInside(relx, rely))
                    {
                        MouseOverCursor = comp.MouseOverCursor;
                        
                        found = true;
                    }
                }   
            }
        }

        public override void OnMouseMove(ICoreClientAPI api, MouseEvent args)
        {
            MouseEvent relArgs = new MouseEvent((int)(args.X - Bounds.absX), (int)(args.Y - Bounds.absY), args.Button);

            for (int i = 0; i < Components.Length; i++)
            {
                Components[i].OnMouseMove(relArgs);
                if (relArgs.Handled) break;
            }

            args.Handled = relArgs.Handled;
        }

        public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
        {
            MouseEvent relArgs = new MouseEvent((int)(args.X - Bounds.absX), (int)(args.Y - Bounds.absY), args.Button);

            for (int i = 0; i < Components.Length; i++)
            {
                Components[i].OnMouseDown(relArgs);
                if (relArgs.Handled) break;
            }

            args.Handled = relArgs.Handled;
        }

        public override void OnMouseUp(ICoreClientAPI api, MouseEvent args)
        {
            MouseEvent relArgs = new MouseEvent((int)(args.X - Bounds.absX), (int)(args.Y - Bounds.absY), args.Button);

            for (int i = 0; i < Components.Length; i++)
            {
                Components[i].OnMouseUp(relArgs);
                if (relArgs.Handled) break;
            }

            args.Handled |= relArgs.Handled;

            base.OnMouseUp(api, args);
        }


        /// <summary>
        /// Recomposes the element for lines.
        /// </summary>
        public void RecomposeText()
        {
            CalcHeightAndPositions();
            Bounds.CalcWorldBounds();

            ImageSurface surface = new ImageSurface(Format.Argb32, (int)Bounds.InnerWidth, (int)Bounds.InnerHeight);
            Context ctx = genContext(surface);
            ComposeFor(Bounds.CopyOnlySize(), ctx, surface);
            
            generateTexture(surface, ref richtTextTexture);

            ctx.Dispose();
            surface.Dispose();
        }

        public void SetNewText(string vtmlCode, CairoFont baseFont, Common.Action<LinkTextComponent> didClickLink = null)
        {
            SetNewTextWithoutRecompose(vtmlCode, baseFont, didClickLink);
            RecomposeText();
        }

        public void SetNewText(RichTextComponentBase[] comps)
        {
            this.Components = comps;
            RecomposeText();
        }

        public void SetNewTextWithoutRecompose(string vtmlCode, CairoFont baseFont, Common.Action<LinkTextComponent> didClickLink = null)
        {
            if (this.Components != null)
            {
                foreach (var val in Components)
                {
                    val?.Dispose();
                }
            }

            this.Components = VtmlUtil.Richtextify(api, vtmlCode, baseFont, didClickLink);
        }

        public void RecomposeInto(ImageSurface surface, Context ctx)
        {
            ComposeFor(Bounds.CopyOnlySize(), ctx, surface);
            generateTexture(surface, ref richtTextTexture);
        }


        public override void Dispose()
        {
            base.Dispose();

            richtTextTexture?.Dispose();

            foreach (var val in Components)
            {
                val.Dispose();
            }
        }
    }




    public static partial class GuiComposerHelpers
    {
        /// <summary>
        /// Adds a rich text element to the GUI
        /// </summary>
        /// <param name="composer"></param>
        /// <param name="vtmlCode"></param>
        /// <param name="baseFont"></param>
        /// <param name="bounds"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static GuiComposer AddRichtext(this GuiComposer composer, string vtmlCode, CairoFont baseFont, ElementBounds bounds, string key = null)
        {
            if (!composer.composed)
            {
                composer.AddInteractiveElement(new GuiElementRichtext(composer.Api, VtmlUtil.Richtextify(composer.Api, vtmlCode, baseFont), bounds), key);
            }

            return composer;
        }


        /// <summary>
        /// Adds a rich text element to the GUI
        /// </summary>
        /// <param name="composer"></param>
        /// <param name="vtmlCode"></param>
        /// <param name="baseFont"></param>
        /// <param name="bounds"></param>
        /// <param name="didClickLink"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static GuiComposer AddRichtext(this GuiComposer composer, string vtmlCode, CairoFont baseFont, ElementBounds bounds, API.Common.Action<LinkTextComponent> didClickLink, string key = null)
        {
            if (!composer.composed)
            {
                composer.AddInteractiveElement(new GuiElementRichtext(composer.Api, VtmlUtil.Richtextify(composer.Api, vtmlCode, baseFont, didClickLink), bounds), key);
            }

            return composer;
        }

        /// <summary>
        /// Adds a rich text element to the GUI
        /// </summary>
        /// <param name="composer"></param>
        /// <param name="components"></param>
        /// <param name="bounds"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static GuiComposer AddRichtext(this GuiComposer composer, RichTextComponentBase[] components, ElementBounds bounds, string key = null)
        {
            if (!composer.composed)
            {
                composer.AddInteractiveElement(new GuiElementRichtext(composer.Api, components, bounds), key);
            }

            return composer;
        }

        /// <summary>
        /// Gets the chat input by name.
        /// </summary>
        /// <param name="composer"></param>
        /// <param name="key">The name of the chat input component.</param>
        /// <returns>The named component.</returns>
        public static GuiElementRichtext GetRichtext(this GuiComposer composer, string key)
        {
            return (GuiElementRichtext)composer.GetElement(key);
        }



    }
}
