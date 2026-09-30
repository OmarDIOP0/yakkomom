using Microsoft.AspNetCore.Mvc;
using Yakkomom.Services.Interfaces;
using Yakkomom.Stockage;

namespace Yakkomom.Controllers;

/// <summary>
/// Documents fonciers côté public : uniquement ceux marqués « Visible par le public »
/// ET dont le terrain est visible sur le site. Tout le reste répond 404, comme s'il n'existait pas.
/// </summary>
public class DocumentsController(IMediaService medias) : Controller
{
    [HttpGet("documents/{id:int}")]
    public async Task<IActionResult> Ouvrir(int id, CancellationToken ct)
    {
        var ouvert = await medias.OuvrirDocumentAsync(id, accesAdmin: false, ct);
        return ouvert is null ? NotFound() : ServirDocument.Reponse(this, ouvert.Value.Document, ouvert.Value.Fichier);
    }
}
