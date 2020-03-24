import * as React from "react";
import { Position } from '@equinor/fusion';
import { ModalSideSheet } from '@equinor/fusion-components';
import useCurrentPosition from './hooks/useCurrentPosition';

type PositionDetailsSideSheetProps = {
    positions: Position[] | null;
}

const PositionDetailsSideSheet:React.FC<PositionDetailsSideSheetProps> = ({positions}) => {
    const { currentPosition, setCurrentPosition } = useCurrentPosition(positions);

    const showSideSheet = React.useMemo(() => currentPosition !== null, [currentPosition]);

    const onClose = React.useCallback(() => {
        setCurrentPosition(null);
    }, [setCurrentPosition]);

    if (!currentPosition) {
        return null;
    }

    return <ModalSideSheet show={showSideSheet} onClose={onClose} header={currentPosition.name}>
        <div>
            side sheet
        </div>
    </ModalSideSheet>
}

export default PositionDetailsSideSheet