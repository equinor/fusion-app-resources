import { ModalSideSheet } from '@equinor/fusion-components';

import { FC, useCallback } from 'react';
import Personnel from '../../../../../../../models/Personnel';
import ReplaceUserSideSheetContent from './ReplaceUserSideSheetContent';

type ReplaceUserSideSheetProps = {
    isOpen: boolean;
    person: Personnel;
    setIsOpen: (state: boolean) => void;
};

const ReplaceUserSideSheet: FC<ReplaceUserSideSheetProps> = ({ isOpen, person, setIsOpen }) => {
    const closeSideSheet = useCallback(() => setIsOpen(false), [setIsOpen]);
    return (
        <ModalSideSheet
            header={`Replace ${person.firstName} ${person.lastName}s person reference`}
            show={isOpen}
            size={'large'}
            onClose={closeSideSheet}
        >
            <ReplaceUserSideSheetContent person={person} onReplacementSuccess={closeSideSheet} />
        </ModalSideSheet>
    );
};
export default ReplaceUserSideSheet;
