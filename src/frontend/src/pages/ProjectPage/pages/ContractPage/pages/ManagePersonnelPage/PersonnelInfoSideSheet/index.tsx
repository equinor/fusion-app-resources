import * as React from 'react';
import { ModalSideSheet, Tabs, Tab } from '@equinor/fusion-components';
import Personnel from '../../../../../../../models/Personnel';
import GeneralTab from './GeneralTab';
import PositionsTab from './PositionsTab';

type PersonnelInfoSideSheetProps = {
    isOpen: boolean;
    person: Personnel;
    setIsOpen: (state: boolean) => void;
};

const PersonnelInfoSideSheet: React.FC<PersonnelInfoSideSheetProps> = ({
    isOpen,
    person,
    setIsOpen,
}) => {

    const [activeTabKey, setActiveTabKey] = React.useState<string>('general');

    return (
        <ModalSideSheet
            header={`Disciplines`}
            show={isOpen}
            size={'large'}
            onClose={() => {
                setIsOpen(false);
            }}
        >
            <Tabs activeTabKey={activeTabKey} onChange={setActiveTabKey}>
                <Tab tabKey="general" title="General">
                    <GeneralTab person={person} />
                </Tab>
                <Tab tabKey="positions" title="Positions">
                    <PositionsTab person={person} />
                </Tab>
            </Tabs>
        </ModalSideSheet>
    );
};

export default PersonnelInfoSideSheet;
