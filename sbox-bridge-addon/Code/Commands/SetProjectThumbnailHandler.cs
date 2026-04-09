using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace SboxBridge;

/// <summary>
/// Sets or updates the project thumbnail image for publishing.
/// Copies the specified image to the standard thumbnail location (thumb.png)
/// in the project root. Supports PNG and JPG formats.
/// </summary>
public class SetProjectThumbnailHandler : ICommandHandler
{
	public Task<object> Execute( JsonElement parameters )
	{
		var project = Project.Current;
		if ( project == null )
			throw new Exception( "No project is currently open" );

		var projectRoot = project.GetRootPath();
		if ( !projectRoot.EndsWith( Path.DirectorySeparatorChar ) )
			projectRoot += Path.DirectorySeparatorChar;

		// Source can be a relative path within the project, or base64 data
		if ( parameters.TryGetProperty( "sourcePath", out var sourceProp ) &&
		     sourceProp.ValueKind == JsonValueKind.String )
		{
			var sourcePath = sourceProp.GetString()
				?? throw new Exception( "sourcePath was null" );

			var fullSourcePath = Path.GetFullPath( Path.Combine( projectRoot, sourcePath ) );
			if ( !fullSourcePath.StartsWith( projectRoot ) )
				throw new Exception( "Source path must be within the project directory" );

			if ( !File.Exists( fullSourcePath ) )
				throw new Exception( $"Source file not found: {sourcePath}" );

			// Determine target name based on file extension
			var ext = Path.GetExtension( fullSourcePath ).ToLowerInvariant();
			if ( ext != ".png" && ext != ".jpg" && ext != ".jpeg" )
				throw new Exception( "Thumbnail must be PNG or JPG format" );

			var targetPath = Path.Combine( projectRoot, "thumb.png" );

			// If source is JPG, just copy as-is with .jpg extension
			if ( ext == ".jpg" || ext == ".jpeg" )
				targetPath = Path.Combine( projectRoot, "thumb.jpg" );

			File.Copy( fullSourcePath, targetPath, true );

			return Task.FromResult<object>( new
			{
				success = true,
				thumbnailPath = Path.GetFileName( targetPath ),
				source = sourcePath,
				message = $"Thumbnail set from {sourcePath}",
			} );
		}

		// Base64 mode: write raw image data
		if ( parameters.TryGetProperty( "base64", out var base64Prop ) &&
		     base64Prop.ValueKind == JsonValueKind.String )
		{
			var base64Data = base64Prop.GetString()
				?? throw new Exception( "base64 data was null" );

			var format = "png";
			if ( parameters.TryGetProperty( "format", out var formatProp ) &&
			     formatProp.ValueKind == JsonValueKind.String )
			{
				format = formatProp.GetString()?.ToLowerInvariant() ?? "png";
			}

			if ( format != "png" && format != "jpg" && format != "jpeg" )
				throw new Exception( "Format must be 'png' or 'jpg'" );

			var targetPath = Path.Combine( projectRoot, $"thumb.{format}" );
			var imageBytes = Convert.FromBase64String( base64Data );
			File.WriteAllBytes( targetPath, imageBytes );

			return Task.FromResult<object>( new
			{
				success = true,
				thumbnailPath = Path.GetFileName( targetPath ),
				source = "base64",
				sizeBytes = imageBytes.Length,
				message = $"Thumbnail written ({imageBytes.Length} bytes)",
			} );
		}

		throw new Exception( "Must provide either 'sourcePath' (relative path to image) or 'base64' (base64-encoded image data)" );
	}
}
