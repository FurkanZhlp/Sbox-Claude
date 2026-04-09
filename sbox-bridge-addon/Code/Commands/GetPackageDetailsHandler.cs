using System;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace SboxBridge;

/// <summary>
/// Fetches detailed package information from the s&box asset library (asset.party).
/// Returns metadata including title, description, version, author, download count,
/// ratings, and dependencies for a given package ident.
/// </summary>
public class GetPackageDetailsHandler : ICommandHandler
{
	public async Task<object> Execute( JsonElement parameters )
	{
		var ident = parameters.GetProperty( "ident" ).GetString()
			?? throw new Exception( "Missing required parameter: ident" );

		// Fetch package from the s&box cloud
		var package = await Package.FetchAsync( ident, false );
		if ( package == null )
			throw new Exception( $"Package not found: {ident}" );

		return new
		{
			ident = package.FullIdent,
			title = package.Title,
			description = package.Description,
			type = package.PackageType.ToString(),
			version = package.Version,
			author = package.Author,
			org = package.Org,
			thumb = package.Thumb,
			tags = package.Tags,
			downloads = package.Downloads,
			favourites = package.Favourites,
			rating = package.Rating,
			updated = package.Updated?.ToString( "o" ),
			created = package.Created?.ToString( "o" ),
			// Dependencies / references
			packageReferences = package.PackageReferences,
		};
	}
}
