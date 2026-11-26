#ifndef TEMPLE_PORTAL_ARCH_INCLUDED
#define TEMPLE_PORTAL_ARCH_INCLUDED

// Pointed (gothic) arch that fills the Temple of the Raven SecretDoor opening.
// p    : door space in metres, x from the centre line, y up from the floor.
// size : door width / height in metres (the SecretDoor cube scale).
// Returns the signed distance in metres to the arch outline, negative inside.

float PortalArchHalfWidth(float2 size)
{
    return size.x * 0.5 - 0.2; // leaves a thin stone border on each side
}

float PortalArchSDF(float2 p, float2 size)
{
    float a = PortalArchHalfWidth(size);
    float R = a * 2.0;              // arc radius > half width => pointed apex
    float rise = a * 1.7320508;     // sqrt(R^2 - (R - a)^2)
    float springY = size.y - 1.25 - rise; // top ~0.9 m of the box is behind the wall ledge

    float2 q = float2(abs(p.x), p.y);
    float side = q.x - a;
    float arc = length(q - float2(a - R, springY)) - R;
    return q.y > springY ? arc : side;
}

#endif
