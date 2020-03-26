import * as React from 'react';
import { useHistory, Position } from '@equinor/fusion';
import { parseQueryString } from '../../../../../../../api/utils';

export default (positions: Position[] | null) => {
    const history = useHistory();
    const [currentPosition, setCurrentPosition] = React.useState<Position | null>(null);

    React.useEffect(() => {
        if (!positions) {
            return;
        }
        const params = parseQueryString(history.location.search);
        const selectedRequest = positions.find(r => r.id === params.positionId);
        setCurrentPosition(selectedRequest || null);
    }, [history.location.search, positions]);

    React.useEffect(() => {
        if(currentPosition === null){
            history.push({
                pathname: history.location.pathname,
                search: "",
            });
        }
    },[currentPosition])

    return {currentPosition, setCurrentPosition};
};
