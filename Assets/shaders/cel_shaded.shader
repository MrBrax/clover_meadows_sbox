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
	#include "common/classes/Light.hlsl"

	RenderState( CullMode, F_RENDER_BACKFACES ? NONE : DEFAULT );

	float g_flDiffuseSteps < Default( 4.0 ); Range( 2.0, 8.0 ); UiGroup( "Cel,10/10" ); >;
	float g_flSpecularSteps < Default( 2.0 ); Range( 1.0, 6.0 ); UiGroup( "Cel,10/20" ); >;
	float g_flIndirectDiffuseStrength < Default( 0.35 ); Range( 0.0, 2.0 ); UiGroup( "Cel,10/30" ); >;
	float g_flIndirectSpecularStrength < Default( 0.15 ); Range( 0.0, 2.0 ); UiGroup( "Cel,10/40" ); >;
	float g_flSpecularStrength < Default( 0.2 ); Range( 0.0, 2.0 ); UiGroup( "Cel,10/50" ); >;
	float g_flRimStrength < Default( 0.15 ); Range( 0.0, 2.0 ); UiGroup( "Cel,10/60" ); >;
	float g_flRimPower < Default( 3.0 ); Range( 0.5, 8.0 ); UiGroup( "Cel,10/70" ); >;
	float3 g_vRimTint < UiType( Color ); Default3( 1.0, 1.0, 1.0 ); UiGroup( "Cel,10/80" ); >;

	float QuantizeBand( float valueInput, float stepsInput )
	{
		float value = max( valueInput, 0.0 );
		float steps = max( round( stepsInput ), 1.0 );

		if ( steps <= 1.0 )
			return value;

		float whole = floor( value );
		float fractional = saturate( value - whole );
		float bands = steps - 1.0;
		float scaled = fractional * bands;
		float lower = floor( scaled );
		float edge = scaled - lower;
		float width = max( fwidth( scaled ), 0.0001 );

		if ( fractional >= 1.0 )
			return whole + 1.0;

		return whole + ( ( lower + smoothstep( 0.5 - width, 0.5 + width, edge ) ) / bands );
	}

	void AccumulateCelDirectLight(
		inout float3 diffuseLighting,
		inout float3 specularLighting,
		float3 normal,
		float3 viewDir,
		float3 lightDir,
		float3 lightColor,
		float attenuation,
		float visibility,
		float roughness,
		float diffuseSteps,
		float specularSteps )
	{
		float lightMask = attenuation * visibility;
		if ( lightMask <= 0.0 )
			return;

		float NdotL = saturate( dot( normal, lightDir ) );
		if ( NdotL <= 0.0 )
			return;

		float diffuseBand = QuantizeBand( NdotL, diffuseSteps );
		diffuseLighting += lightColor * lightMask * diffuseBand;

		float specularPower = lerp( 128.0, 8.0, roughness );
		float3 halfDir = normalize( lightDir + viewDir + 0.0001 );
		float specularTerm = pow( saturate( dot( normal, halfDir ) ), specularPower ) * NdotL;
		float specularBand = QuantizeBand( specularTerm, specularSteps );
		specularLighting += lightColor * lightMask * specularBand;
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

		CalculateIndirectLighting( lightingTerms, combinerInput );

		float3 vDiffuseAO = CalculateDiffuseAmbientOcclusion( combinerInput, lightingTerms );
		lightingTerms.vIndirectDiffuse.rgb *= vDiffuseAO.rgb;

		float3 vSpecularAO = CalculateSpecularAmbientOcclusion( combinerInput, lightingTerms );
		lightingTerms.vIndirectSpecular.rgb *= vSpecularAO.rgb;

		float3 viewDir = normalize( g_vCameraPositionWs - m.WorldPosition );
		float3 directDiffuse = 0.0f;
		float3 directSpecular = 0.0f;

		// Quantize from geometric light response first, then apply real shadow visibility.
		// This keeps CSM edges from fighting the cel-band threshold.
		if ( g_DirectionalLightEnabled )
		{
			float3 sunDir = -normalize( g_DirectionalLightDirection.xyz );
			float3 sunColor = g_DirectionalLightColor.rgb;
			float sunVisibility = DirectionalLightShadow::GetVisibility( m.WorldPosition, m.ScreenPosition.xy );
			AccumulateCelDirectLight( directDiffuse, directSpecular, m.Normal, viewDir, sunDir, sunColor, 1.0, sunVisibility, m.Roughness, g_flDiffuseSteps, g_flSpecularSteps );
		}

		uint lightCount = Light::Count( m.ScreenPosition );
		[loop]
		for ( uint li = 0; li < lightCount; li++ )
		{
			Light light = Light::From( m.WorldPosition, m.ScreenPosition, li, m.LightmapUV );
			if ( light.Attenuation <= 0.0 )
				continue;

			AccumulateCelDirectLight( directDiffuse, directSpecular, m.Normal, viewDir, light.Direction, light.Color, light.Attenuation, light.Visibility, m.Roughness, g_flDiffuseSteps, g_flSpecularSteps );
		}

		directDiffuse *= lerp( float3( 1.0, 1.0, 1.0 ), vDiffuseAO.rgb, combinerInput.flAmbientOcclusionDirectDiffuse );
		directSpecular *= lerp( float3( 1.0, 1.0, 1.0 ), vSpecularAO.rgb, combinerInput.flAmbientOcclusionDirectSpecular );

		directSpecular *= g_flSpecularStrength;
		float3 indirectDiffuse = lightingTerms.vIndirectDiffuse.rgb * g_flIndirectDiffuseStrength;
		float3 indirectSpecular = lightingTerms.vIndirectSpecular.rgb * g_flIndirectSpecularStrength;
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
