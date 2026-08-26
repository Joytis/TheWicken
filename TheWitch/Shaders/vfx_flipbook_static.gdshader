shader_type canvas_item;
render_mode blend_mix;

#include "res://shaders/vfx/_util/flipbook.gdshaderinc"
#include "res://shaders/vfx/_util/hue_shift.gdshaderinc"
#include "res://shaders/vfx/_util/erosion_from_factors.gdshaderinc"

uniform vec2 pivot_offset = vec2(0.0, 0.0);


// Which cell of the flipbook to show (0-based, row-major). Set per instance from C#.
uniform float flipbook_offset = 0.0;
uniform vec2 flipbook_size = vec2(3,3);

varying float lifetime;
varying vec4 vertex_color;

varying vec2 flipbook_uv;

void vertex() {
	vertex_color = COLOR;
	lifetime = INSTANCE_CUSTOM.y/INSTANCE_CUSTOM.w;
	VERTEX += pivot_offset * (1.0/TEXTURE_PIXEL_SIZE);
	
	flipbook_uv = flipbook(UV, flipbook_size, flipbook_offset);
}

void fragment() {
	vec4 flipbook_texture_color =  texture(TEXTURE, flipbook_uv);

	float alpha = flipbook_texture_color.a * vertex_color.a;

    vec4 final_color = vertex_color;
	
	COLOR = vec4(final_color.rgb, alpha);
}