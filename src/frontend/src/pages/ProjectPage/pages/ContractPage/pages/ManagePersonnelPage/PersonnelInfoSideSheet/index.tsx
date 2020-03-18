import * as React from 'react';
import { ModalSideSheet } from '@equinor/fusion-components';
import Personnel from '../../../../../../../models/Personnel';

type PersonnelInfoSideSheetProps = {
    isOpen: boolean;
    person: Personnel | null;
    setIsOpen: (state: boolean) => void;
};

const PersonnelInfoSideSheet: React.FC<PersonnelInfoSideSheetProps> = ({
    isOpen,
    person,
    setIsOpen,
}) => {
    return (
        <ModalSideSheet
            header={`${person?.firstName} ${person?.lastName}`}
            show={isOpen}
            size={'large'}
            onClose={() => {
                setIsOpen(false);
            }}
        >
            <div>{person?.firstName}</div>
            <div>{person?.lastName}</div>
        </ModalSideSheet>
    );
};

export default PersonnelInfoSideSheet;
