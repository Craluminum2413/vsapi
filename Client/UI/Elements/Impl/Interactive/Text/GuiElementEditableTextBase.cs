﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cairo;
using Vintagestory.API.MathTools;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client
{
    public abstract class GuiElementEditableTextBase : GuiElementTextBase
    {
        internal float[] caretColor = new float[] { 1, 1, 1, 1 };

        internal bool hideCharacters;
        internal bool multilineMode;
        internal int maxlines = 99999;
        internal int maxwidth = -1;

        internal int caretPosLine;
        internal int caretPosInLine;

        internal double caretX, caretY;
        internal double topPadding;
        internal double leftPadding = 3;
        internal double rightSpacing;
        internal double bottomSpacing;

       // internal int selectedTextStart;
        //internal int selectedTextEnd;

        internal LoadedTexture caretTexture;
        internal LoadedTexture textTexture;
        //internal int selectionTextureId;

        public Action<string> OnTextChanged;
        public Action<double, double> OnCursorMoved;

        internal Action OnFocused = null;
        internal Action OnLostFocus = null;

        /// <summary>
        /// Called when a keyboard key was pressed, received and handled
        /// </summary>
        public Action OnKeyPressed;


        internal long caretBlinkMilliseconds;
        internal bool caretDisplayed;
        internal double caretHeight;

        internal double renderLeftOffset;
        internal Vec2i textSize = new Vec2i();

        internal List<string> lines;


        public int TextLengthWithoutLineBreaks {
            get {
                int length = 0;
                for (int i = 0; i < lines.Count; i++) length += lines[i].Length;
                return length;
            }
        }

        public int CaretPosWithoutLineBreaks
        {
            get
            {
                int pos = 0;
                for (int i = 0; i < caretPosLine; i++) pos += lines[i].Length;
                return pos + caretPosInLine;
            }
        }

        public int CaretPosLine => caretPosLine;
        public int CaretPosInLine => caretPosInLine;

        public override bool Focusable
        {
            get { return true; }
        }

        /// <summary>
        /// Initializes the text component.
        /// </summary>
        /// <param name="capi">The Client API</param>
        /// <param name="font">The font of the text.</param>
        /// <param name="bounds">The bounds of the component.</param>
        public GuiElementEditableTextBase(ICoreClientAPI capi, CairoFont font, ElementBounds bounds) : base(capi, "", font, bounds)
        {
            caretTexture = new LoadedTexture(capi);
            textTexture = new LoadedTexture(capi);

            lines = new List<string>
            {
                ""
            };
        }

        public override void OnFocusGained()
        {
            base.OnFocusGained();
            SetCaretPos(TextLengthWithoutLineBreaks);
            OnFocused?.Invoke();
        }

        public override void OnFocusLost()
        {
            base.OnFocusLost();
            OnLostFocus?.Invoke();
        }

        /// <summary>
        /// Sets the position of the cursor at a given point.
        /// </summary>
        /// <param name="x">X position of the cursor.</param>
        /// <param name="y">Y position of the cursor.</param>
        public void SetCaretPos(double x, double y)
        {
            caretPosLine = 0;

            ImageSurface surface = new ImageSurface(Format.Argb32, 1, 1);
            Context ctx = genContext(surface);
            Font.SetupContext(ctx);

            if (multilineMode)
            {
                double lineY = y / ctx.FontExtents.Height;
                if (lineY > lines.Count)
                {
                    caretPosLine = lines.Count - 1;
                    caretPosInLine = lines[caretPosLine].Length;

                    ctx.Dispose();
                    surface.Dispose();
                    return;
                }

                caretPosLine = Math.Max(0, (int)lineY);
            }

            string line = lines[caretPosLine];
            caretPosInLine = line.Length;

            for (int i = 0; i < line.Length; i++)
            {
                double posx = ctx.TextExtents(line.Substring(0, i+1)).XAdvance;

                if (x - posx <= 0)
                {
                    caretPosInLine = i;
                    break;
                }
            }

            ctx.Dispose();
            surface.Dispose();

            SetCaretPos(caretPosInLine, caretPosLine);
        }


       
        /// <summary>
        /// Sets the position of the cursor to a specific character.
        /// </summary>
        /// <param name="posInLine">The position in the line.</param>
        /// <param name="posLine">The line of the text.</param>
        public void SetCaretPos(int posInLine, int posLine = 0)
        {
            caretBlinkMilliseconds = api.ElapsedMilliseconds;
            caretDisplayed = true;

            caretPosLine = GameMath.Clamp(posLine, 0, lines.Count - 1);
            caretPosInLine = GameMath.Clamp(posInLine, 0, lines[caretPosLine].Length);


            if (multilineMode)
            {
                caretX = Font.GetTextExtents(lines[caretPosLine].Substring(0, caretPosInLine)).XAdvance;
                caretY = Font.GetFontExtents().Height * caretPosLine;
            }
            else
            {
                string displayedText = lines[0];

                if (hideCharacters)
                {
                    displayedText = new StringBuilder(lines[0]).Insert(0, "•", displayedText.Length).ToString();
                }

                caretX = Font.GetTextExtents(displayedText.Substring(0, caretPosInLine)).XAdvance;
                caretY = 0;
            }

            OnCursorMoved?.Invoke(caretX, caretY);

            renderLeftOffset = Math.Max(0, caretX - Bounds.InnerWidth + rightSpacing);
        }

        /// <summary>
        /// Sets a numerical value to the text, appending it to the end of the text.
        /// </summary>
        /// <param name="value">The value to add to the text.</param>
        public void SetValue(float value)
        {
            SetValue(value.ToString(GlobalConstants.DefaultCultureInfo));
        }

        /// <summary>
        /// Sets given text, sets the cursor to the end of the text
        /// </summary>
        /// <param name="text"></param>
        public void SetValue(string text)
        {
            LoadValue(text);
            SetCaretPos(lines[lines.Count - 1].Length, lines.Count - 1);
        }

        /// <summary>
        /// Sets given texts, leaves cursor position unchanged
        /// </summary>
        /// <param name="text"></param>
        public void LoadValue(string text)
        {
            if (text == null) text = "";

            // We only allow Linux style newlines (only \n)
            text = text.Replace("\r\n", "\n").Replace('\r', '\n');

            ImageSurface surface = new ImageSurface(Format.Argb32, 1, 1);
            Context ctx = genContext(surface);
            Font.SetupContext(ctx);

            if (multilineMode)
            {
                TextLine[] textlines = textUtil.Lineize(Font, text, Bounds.InnerWidth - 2 * Bounds.absPaddingX);
                lines.Clear();
                foreach (var val in textlines) lines.Add(val.Text);

                if (lines.Count == 0)
                {
                    lines.Add("");
                }
            }
            else
            {
                lines[0] = text;
            }

            while (lines.Count > maxlines) {
                lines.RemoveAt(lines.Count - 1);
            }

            RecomposeText();
            TextChanged();

            ctx.Dispose();
            surface.Dispose();
        }


        internal virtual void TextChanged()
        {
            OnTextChanged?.Invoke(string.Join("\n", lines));
            RecomposeText();
        }

        internal virtual void RecomposeText()
        {
            Bounds.CalcWorldBounds();


            string displayedText = null;
            ImageSurface surface;

            if (multilineMode) {
                textSize.X = (int)(Bounds.OuterWidth - rightSpacing);
                textSize.Y = (int)(Bounds.OuterHeight - bottomSpacing);
                
            } else {
                displayedText = lines[0];

                if (hideCharacters)
                {
                    displayedText = new StringBuilder(displayedText.Length).Insert(0, "•", displayedText.Length).ToString();
                }

                textSize.X = (int)Math.Max(Bounds.InnerWidth - rightSpacing, Font.GetTextExtents(displayedText).Width);
                textSize.Y = (int)(Bounds.InnerHeight - bottomSpacing);
            }


            surface = new ImageSurface(Format.Argb32, textSize.X, textSize.Y);

            Context ctx = genContext(surface);
            Font.SetupContext(ctx);

            double fontHeight = ctx.FontExtents.Height;
            double topPadding = Math.Max(0, Bounds.OuterHeight - bottomSpacing - fontHeight) / 2;
            
            if (multilineMode)
            {
                double width = Bounds.InnerWidth - 2 * Bounds.absPaddingX - rightSpacing;

                TextLine[] textlines = new TextLine[lines.Count];
                for (int i = 0; i < textlines.Length; i++)
                {
                    textlines[i] = new TextLine()
                    {
                        Text = lines[i],
                        Bounds = new LineRectangled(0, i*fontHeight, Bounds.InnerWidth, fontHeight)
                    };
                }

                textUtil.DrawMultilineTextAt(ctx, Font, textlines, Bounds.absPaddingX + leftPadding, Bounds.absPaddingY, width, EnumTextOrientation.Left);
            } else
            {
                this.topPadding = Math.Max(0, Bounds.OuterHeight - bottomSpacing - ctx.FontExtents.Height) / 2;
                textUtil.DrawTextLine(ctx, Font, displayedText, Bounds.absPaddingX + leftPadding, Bounds.absPaddingY + this.topPadding);
            }


            generateTexture(surface, ref textTexture);
            ctx.Dispose();
            surface.Dispose();

            if (caretTexture.TextureId == 0)
            {
                caretHeight = fontHeight;
                surface = new ImageSurface(Format.Argb32, (int)3.0, (int)fontHeight);
                ctx = genContext(surface);
                Font.SetupContext(ctx);

                ctx.SetSourceRGBA(caretColor[0], caretColor[1], caretColor[2], caretColor[3]);
                ctx.LineWidth = 1;
                ctx.NewPath();
                ctx.MoveTo(2, 0);
                ctx.LineTo(2, fontHeight);
                ctx.ClosePath();
                ctx.Stroke();

                generateTexture(surface, ref caretTexture.TextureId);

                ctx.Dispose();
                surface.Dispose();
            }
        }


        #region Mouse, Keyboard


        public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
        {
            base.OnMouseDownOnElement(api, args);

            SetCaretPos(args.X - Bounds.absX, args.Y - Bounds.absY);
        }

        public override void OnKeyDown(ICoreClientAPI api, KeyEvent args)
        {
            if (HasFocus)
            {
                bool handled = multilineMode || args.KeyCode != (int)GlKeys.Tab;

                if (args.KeyCode == (int)GlKeys.BackSpace)
                {
                    if(CaretPosWithoutLineBreaks > 0) OnKeyBackSpace();
                }

                if (args.KeyCode == (int)GlKeys.Delete)
                {
                    if (CaretPosWithoutLineBreaks < TextLengthWithoutLineBreaks) OnKeyDelete();
                }

                if (args.KeyCode == (int)GlKeys.End)
                {
                    if (args.CtrlPressed)
                    {
                        SetCaretPos(lines[lines.Count - 1].Length, lines.Count - 1);
                    } else
                    {
                        SetCaretPos(lines[caretPosLine].Length, caretPosLine);
                    }
                    
                    api.Gui.PlaySound("tick");
                }

                if (args.KeyCode == (int)GlKeys.Home)
                {
                    if (args.CtrlPressed)
                    {
                        SetCaretPos(0);
                    } else
                    {
                        SetCaretPos(0, caretPosLine);
                    }
                    
                    api.Gui.PlaySound("tick");
                }

                if (args.KeyCode == (int)GlKeys.Left)
                {
                    MoveCursor(-1, args.CtrlPressed);
                }

                if (args.KeyCode == (int)GlKeys.Right)
                {
                    MoveCursor(1, args.CtrlPressed);
                }

                if (args.KeyCode == (int)GlKeys.V && (args.CtrlPressed || args.CommandPressed))
                {
                    string insert = api.Forms.GetClipboardText();
                    insert = insert.Replace("\uFEFF", ""); // UTF-8 bom, we don't need that one, like ever

                    string fulltext = string.Join("\n", lines);

                    int caretPos = caretPosInLine;
                    for (int i = 0; i < caretPosLine; i++)
                    {
                        caretPos += lines[i].Length + 1;
                    }

                    SetValue(fulltext.Substring(0, caretPos) + insert + fulltext.Substring(caretPos, fulltext.Length - caretPos));
                    api.Gui.PlaySound("tick");
                }

                if (args.KeyCode == (int)GlKeys.Down && caretPosLine < lines.Count - 1)
                {
                    SetCaretPos(caretPosInLine, caretPosLine + 1);
                    api.Gui.PlaySound("tick");
                }

                if (args.KeyCode == (int)GlKeys.Up && caretPosLine > 0)
                {
                    SetCaretPos(caretPosInLine, caretPosLine - 1);
                    api.Gui.PlaySound("tick");
                }

                if (args.KeyCode == (int)GlKeys.Enter || args.KeyCode == (int)GlKeys.KeypadEnter)
                {
                    if (multilineMode)
                    {
                        OnKeyEnter();
                    } else
                    {
                        handled = false;
                    }
                }

                if (args.KeyCode == (int)GlKeys.Escape) handled = false;

                args.Handled = handled;
                
            }            
        }


        public override string GetText()
        {
            return string.Join("\n", lines);
        }

        private void OnKeyEnter()
        {
            if (lines.Count >= maxlines) return;

            string leftText = lines[caretPosLine].Substring(0, caretPosInLine);
            string rightText = lines[caretPosLine].Substring(caretPosInLine);

            lines[caretPosLine] = leftText;
            lines.Insert(caretPosLine + 1, rightText);

            TextChanged();
            SetCaretPos(0, caretPosLine + 1);
            api.Gui.PlaySound("tick");
        }

        private void OnKeyDelete()
        {
            if (caretPosInLine < lines[caretPosLine].Length)
            {
                lines[caretPosLine] = lines[caretPosLine].Substring(0, caretPosInLine) + lines[caretPosLine].Substring(caretPosInLine + 1, lines[caretPosLine].Length - (caretPosInLine + 1));
            }
            else
            {
                if (caretPosLine < lines.Count - 1)
                {
                    lines[caretPosLine] += lines[caretPosLine + 1];
                    lines.RemoveAt(caretPosLine + 1);
                }
            }


            TextChanged();
            api.Gui.PlaySound("tick");
        }

        private void OnKeyBackSpace()
        {
            if (caretPosInLine > 0)
            {
                lines[caretPosLine] = lines[caretPosLine].Substring(0, caretPosInLine - 1) + lines[caretPosLine].Substring(caretPosInLine, lines[caretPosLine].Length - caretPosInLine);
                SetCaretPos(caretPosInLine - 1, caretPosLine);
            }
            else
            {
                if (caretPosLine > 0)
                {
                    int posInLine = lines[caretPosLine - 1].Length;

                    double nowWidth = Font.GetTextExtents(lines[caretPosLine - 1] + lines[caretPosLine]).Width;

                    if (nowWidth <= maxwidth)
                    {
                        lines[caretPosLine - 1] += lines[caretPosLine];
                        lines.RemoveAt(caretPosLine);

                        SetCaretPos(posInLine, caretPosLine - 1);
                    }
                }
            }

            TextChanged();
            api.Gui.PlaySound("tick");
        }

        public override void OnKeyPress(ICoreClientAPI api, KeyEvent args)
        {
            if (HasFocus)
            {
                string nowline = lines[caretPosLine].Substring(0, caretPosInLine) + args.KeyChar + lines[caretPosLine].Substring(caretPosInLine, lines[caretPosLine].Length - caretPosInLine);
                
                if (maxwidth > 0)
                {
                    double nowWidth = Font.GetTextExtents(nowline).Width;

                    if (nowWidth > maxwidth)
                    {
                        args.Handled = true;
                        api.Gui.PlaySound("tick");
                        return;
                    }
                }

                lines[caretPosLine] = nowline;
                TextChanged();
                SetCaretPos(caretPosInLine + 1, caretPosLine);

                args.Handled = true;
                api.Gui.PlaySound("tick");

                OnKeyPressed?.Invoke();
            }
        }



        #endregion


        public override void RenderInteractiveElements(float deltaTime)
        {
            if (HasFocus)
            {
                if (api.ElapsedMilliseconds - caretBlinkMilliseconds > 900)
                {
                    caretBlinkMilliseconds = api.ElapsedMilliseconds;
                    caretDisplayed = !caretDisplayed;
                }

                if (caretDisplayed && caretX - renderLeftOffset < Bounds.InnerWidth)
                {
                    api.Render.Render2DTexturePremultipliedAlpha(caretTexture.TextureId, Bounds.renderX + caretX + scaled(1.5) - renderLeftOffset, Bounds.renderY + caretY + topPadding, 2, caretHeight);
                }

                
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            caretTexture.Dispose();
            textTexture.Dispose();
        }


        /// <summary>
        /// Moves the cursor forward and backward by an amount.
        /// </summary>
        /// <param name="dir">The direction to move the cursor.</param>
        /// <param name="wholeWord">Whether or not we skip entire words moving it.</param>
        public void MoveCursor(int dir, bool wholeWord = false)
        {
            bool done = false;
            bool moved = 
                ((caretPosInLine > 0 || caretPosLine > 0) && dir < 0) ||
                ((caretPosInLine < lines[caretPosLine].Length || caretPosLine < lines.Count-1) && dir > 0)
            ;

            int newPos = caretPosInLine;
            int newLine = caretPosLine;

            while (!done) {
                newPos += dir;

                if (newPos < 0)
                {
                    if (newLine <= 0) break;
                    newLine--;
                    newPos = lines[newLine].Length;
                } 

                if (newPos > lines[newLine].Length)
                {
                    if (newLine >= lines.Count - 1) break;
                    newPos = 0;
                    newLine++;
                }

                done = !wholeWord || (newPos > 0 && lines[newLine][newPos - 1] == ' ');
            }

            if (moved)
            {
                SetCaretPos(newPos, newLine);
                api.Gui.PlaySound("tick");
            }
        }



        /// <summary>
        /// Sets the number of lines in the Text Area.
        /// </summary>
        /// <param name="maxlines">The maximum number of lines.</param>
        public void SetMaxLines(int maxlines)
        {
            this.maxlines = maxlines;
        }


        public void SetMaxWidth(int maxwidth)
        {
            this.maxwidth = maxwidth;
        }
    }

}
