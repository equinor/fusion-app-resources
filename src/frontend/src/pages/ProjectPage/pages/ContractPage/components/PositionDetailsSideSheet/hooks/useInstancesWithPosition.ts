import { Position, PositionInstance } from '@equinor/fusion';
import { useMemo } from 'react';

export type InstanceWithPosition = PositionInstance & {
    position: Position;
};

export default (positions: Position[]) => {
    return useMemo(
        () =>
            positions.reduce<InstanceWithPosition[]>(
                (instances, position) => [
                    ...instances,
                    ...position.instances.map(i => ({
                        ...i,
                        position,
                    })),
                ],
                []
            ),
        [positions]
    );
};
