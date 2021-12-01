﻿using System.Collections.Generic;
using Cairo;
using Vintagestory.API.MathTools;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using System;
using System.Linq;
using Vintagestory.API.Util;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Client
{
    public class GuiHandbookTextPage : GuiHandbookPage
    {
        public string pageCode;
        public string Title;
        public string Text;
        public string categoryCode = "guide";

        public LoadedTexture Texture;
        public override string PageCode => pageCode;

        public override string CategoryCode => categoryCode;

        public override void Dispose() { Texture?.Dispose(); Texture = null; }

        RichTextComponentBase[] comps;
        public int PageNumber;

        string titleCached;
        public override bool IsDuplicate => false;

        public GuiHandbookTextPage()
        {
            
        }

        public void Init(ICoreClientAPI capi)
        {
            if (Text.Length < 255)
            {
                Text = Lang.Get(Text);
            }
            
            comps = VtmlUtil.Richtextify(capi, Text, CairoFont.WhiteSmallText().WithLineHeightMultiplier(1.2));

            titleCached = Lang.Get(Title);
        }

        public override RichTextComponentBase[] GetPageText(ICoreClientAPI capi, ItemStack[] allStacks, ActionConsumable<string> openDetailPageFor)
        {
            return comps;
        }

        public void Recompose(ICoreClientAPI capi)
        {
            Texture?.Dispose();
            Texture = new TextTextureUtil(capi).GenTextTexture(Lang.Get(Title), CairoFont.WhiteSmallText());

            
        }

        public override float TextMatchWeight(string searchText)
        {
            if (titleCached.Equals(searchText, StringComparison.InvariantCultureIgnoreCase)) return 3;
            if (titleCached.StartsWith(searchText, StringComparison.InvariantCultureIgnoreCase)) return 2.5f;
            if (titleCached.CaseInsensitiveContains(searchText)) return 2;
            if (Text.CaseInsensitiveContains(searchText)) return 1;
            return 0;
        }

        public override void RenderTo(ICoreClientAPI capi, double x, double y)
        {
            float size = (float)GuiElement.scaled(25);
            float pad = (float)GuiElement.scaled(10);

            if (Texture == null)
            {
                Recompose(capi);
            }

            capi.Render.Render2DTexturePremultipliedAlpha(
                Texture.TextureId,
                (x + pad),
                y + size / 4 - 3,
                Texture.Width,
                Texture.Height,
                50
            );
        }
    }
}
