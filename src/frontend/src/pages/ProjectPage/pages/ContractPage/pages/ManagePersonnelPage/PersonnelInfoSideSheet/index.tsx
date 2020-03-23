import * as React from 'react';
import { ModalSideSheet, Tabs, Tab, IconButton, EditIcon, DoneIcon } from '@equinor/fusion-components';
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
    const [editMode, setEditMode] = React.useState<boolean>(false);

    const headerIcons = React.useMemo(() => {
        if (activeTabKey !== 'general') return []

        return editMode
            ? [
                <IconButton onClick={() => {
                }}>
                    <DoneIcon />
                </IconButton>
            ]
            : [
                <IconButton onClick={() => setEditMode(true)}>
                    <EditIcon />
                </IconButton>
            ]
    }, [editMode, activeTabKey])

    return (
        <ModalSideSheet
            header={`Disciplines`}
            show={isOpen}
            size={'large'}
            headerIcons={headerIcons}
            onClose={() => {
                setEditMode(false);
                setIsOpen(false);
            }}
        >
            <Tabs activeTabKey={activeTabKey} onChange={setActiveTabKey}>
                <Tab tabKey="general" title="General">
                    <GeneralTab person={person} edit={editMode} setEdit={setEditMode} />
                </Tab>
                <Tab disabled={editMode} tabKey="positions" title="Positions">
                    <PositionsTab person={person} />
                </Tab>
            </Tabs>
        </ModalSideSheet>
    );
};

export default PersonnelInfoSideSheet;
