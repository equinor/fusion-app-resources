import { Position, PositionInstance } from '@equinor/fusion';
import Personnel from './Personnel';

type InstanceWithPersonnel = PositionInstance & {
    personnelDetails?: Personnel;
};
type PositionWithPersonnel = Omit<Position, 'instances'> & {
    instances: InstanceWithPersonnel[];
};

export default PositionWithPersonnel;
