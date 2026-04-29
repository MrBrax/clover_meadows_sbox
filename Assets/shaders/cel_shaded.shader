HEADER
{
	Description = "Cel shading shader that quantizes the engine lighting pipeline.";
	Version = 1;
}

FEATURES
{
	#include "common/features.hlsl"
}

MODES
{
	Forward();
	Depth( S_MODE_DEPTH );
	ToolsShadingComplexity( "tools_shading_complexity.shader" );
}

COMMON
{
	#include "common/shared.hlsl"
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"
};

struct PixelInput
{
	#include "common/pixelinput.hlsl"
};

VS
{
	#include "common/vertex.hlsl"

	PixelInput MainVs( VertexInput i )
	{
		PixelInput o = ProcessVertex( i );
		return FinalizeVertex( o );
	}
}

PS
{
	#include "common/utils/Material.CommonInputs.hlsl"
	#include "common/pixel.hlsl"

	RenderState( CullMode, F_RENDER_BACKFACES ? NONE : DEFAULT );

	float g_flDiffuseSteps < Default( 4.0 ); Range( 1.0, 8.0 ); UiGroup( "Cel,10/10" ); >;
	float g_flSpecularSteps < Default( 2.0 ); Range( 1.0, 6.0 ); UiGroup( "Cel,10/20" ); >;
	float g_flIndirectDiffuseStrength < Default( 0.35 ); Range( 0.0, 2.0 ); UiGroup( "Cel,10/30" ); >;
	float g_flIndirectSpecularStrength < Default( 0.15 ); Range( 0.0, 2.0 ); UiGroup( "Cel,10/40" ); >;
	float g_flSpecularStrength < Default( 0.2 ); Range( 0.0, 2.0 ); UiGroup( "Cel,10/50" ); >;
	float g_flRimStrength < Default( 0.15 ); Range( 0.0, 2.0 ); UiGroup( "Cel,10/60" ); >;
	float g_flRimPower < Default( 3.0 ); Range( 0.5, 8.0 ); UiGroup( "Cel,10/70" ); >;
	float3 g_vRimTint < UiType( Color ); Default3( 1.0, 1.0, 1.0 ); UiGroup( "Cel,10/80" ); >;

	float QuantizeBand( float value, float steps )
	{
		value = max( value, 0.0 );
		steps = max( steps, 1.0 );

		if ( steps <= 1.0 )
			return value;

		return round( value * steps ) / steps;
	}

	float3 QuantizeLight( float3 lightColor, float steps )
	{
		float intensity = max( lightColor.r, max( lightColor.g, lightColor.b ) );
		if ( intensity <= 0.0001 )
			return 0.0f;

		float quantized = QuantizeBand( intensity, steps );
		return lightColor * ( quantized / intensity );
	}

	float4 MainPs( PixelInput i ) : SV_Target0
	{
		Material m = Material::From( i );

		m.AmbientOcclusion = saturate( m.AmbientOcclusion );
		m.Roughness = saturate( m.Roughness );
		m.Metalness = saturate( m.Metalness );
		m.Opacity = saturate( m.Opacity );

		Decals::Apply( m.WorldPosition, m );
		AdjustAlphaToCoverage( m );

		LightingTerms_t lightingTerms = InitLightingTerms();
		CombinerInput combinerInput = ShadingModelStandard::MaterialToCombinerInput( m );

		// Let the engine resolve clustered lights, sun direction, shadows, baked lighting,
		// and indirect probes/envmaps first, then quantize the resulting lighting bands.
		ComputeDirectLighting( lightingTerms, combinerInput );
		CalculateIndirectLighting( lightingTerms, combinerInput );

		float3 vDiffuseAO = CalculateDiffuseAmbientOcclusion( combinerInput, lightingTerms );
		lightingTerms.vIndirectDiffuse.rgb *= vDiffuseAO.rgb;
		lightingTerms.vDiffuse.rgb *= lerp( float3( 1.0, 1.0, 1.0 ), vDiffuseAO.rgb, combinerInput.flAmbientOcclusionDirectDiffuse );

		float3 vSpecularAO = CalculateSpecularAmbientOcclusion( combinerInput, lightingTerms );
		lightingTerms.vIndirectSpecular.rgb *= vSpecularAO.rgb;
		lightingTerms.vSpecular.rgb *= lerp( float3( 1.0, 1.0, 1.0 ), vSpecularAO.rgb, combinerInput.flAmbientOcclusionDirectSpecular );

		float3 directDiffuse = QuantizeLight( lightingTerms.vDiffuse.rgb, g_flDiffuseSteps );
		float3 directSpecular = QuantizeLight( lightingTerms.vSpecular.rgb, g_flSpecularSteps ) * g_flSpecularStrength;
		float3 indirectDiffuse = lightingTerms.vIndirectDiffuse.rgb * g_flIndirectDiffuseStrength;
		float3 indirectSpecular = lightingTerms.vIndirectSpecular.rgb * g_flIndirectSpecularStrength;

		float3 viewDir = normalize( g_vCameraPositionWs - m.WorldPosition );
		float rim = pow( 1.0 - saturate( dot( m.Normal, viewDir ) ), g_flRimPower ) * g_flRimStrength;
		float3 rimLighting = g_vRimTint * rim * ( 1.0 - saturate( max( directDiffuse.r, max( directDiffuse.g, directDiffuse.b ) ) ) );

		float3 diffuse = ( directDiffuse + indirectDiffuse ) * combinerInput.vDiffuseColor.rgb;
		float3 specular = directSpecular + indirectSpecular;

		float4 color = float4( diffuse + specular + combinerInput.vEmissive.rgb + ( rimLighting * combinerInput.vDiffuseColor.rgb ), m.Opacity );

		LightingTerms_t celLightingTerms = lightingTerms;
		celLightingTerms.vDiffuse.rgb = directDiffuse;
		celLightingTerms.vSpecular.rgb = directSpecular;
		celLightingTerms.vIndirectDiffuse.rgb = indirectDiffuse;
		celLightingTerms.vIndirectSpecular.rgb = indirectSpecular;

		if ( DepthNormals::WantsDepthNormals() )
			return DepthNormals::Output( m.Normal, m.Roughness, color.a );

		if ( ToolsVis::WantsToolsVis() )
			return ShadingModelStandard::DoToolsVis( color, m, celLightingTerms );

		if ( g_bWireframeMode )
			return g_vWireframeColor;

		return DoAtmospherics( m.WorldPosition, m.ScreenPosition.xy, color );
	}
}
