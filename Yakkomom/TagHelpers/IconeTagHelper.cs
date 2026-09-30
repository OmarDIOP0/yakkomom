using Microsoft.AspNetCore.Razor.TagHelpers;
using Yakkomom.Services.Interfaces;

namespace Yakkomom.TagHelpers;

/// <summary>
/// &lt;icone nom="whatsapp" /&gt; → &lt;svg class="ic"&gt;&lt;use href="/img/icons.{empreinte}.svg#whatsapp"/&gt;&lt;/svg&gt;.
/// Le sprite est un fichier séparé, mis en cache longtemps grâce à son URL avec empreinte.
/// </summary>
[HtmlTargetElement("icone", TagStructure = TagStructure.WithoutEndTag)]
public class IconeTagHelper(IAssetsStatiques assets) : TagHelper
{
    private const string CheminSprite = "img/icons.svg";

    public required string Nom { get; set; }
    /// <summary>Classes supplémentaires.</summary>
    public string? Classe { get; set; }
    /// <summary>Texte accessible ; sinon l'icône est décorative (aria-hidden).</summary>
    public string? Libelle { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "svg";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", string.IsNullOrEmpty(Classe) ? "ic" : $"ic {Classe}");
        if (string.IsNullOrEmpty(Libelle))
        {
            output.Attributes.SetAttribute("aria-hidden", "true");
            output.Attributes.SetAttribute("focusable", "false");
        }
        else
        {
            output.Attributes.SetAttribute("role", "img");
            output.Attributes.SetAttribute("aria-label", Libelle);
        }
        output.Content.SetHtmlContent($"<use href=\"{assets[CheminSprite]}#{Nom}\"></use>");
    }
}
