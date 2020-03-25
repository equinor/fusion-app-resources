import * as React from 'react';
import { Position, formatDate } from '@equinor/fusion';
import { ModalSideSheet, Tabs, Tab } from '@equinor/fusion-components';
import useCurrentPosition from './hooks/useCurrentPosition';
import * as styles from './styles.less';
import PositionAssignment from './PositionAssignment';
import ReportingPathTab from './tabs/ReportingPathTab';
import PositionTimeline from './components/PositionTimeline';
import DisciplineNetworkTab from './tabs/DisciplineNetworkTab';
import RoleDescriptionTab from './tabs/RoleDescriptionTab';

type PositionDetailsSideSheetProps = {
    positions: Position[] | null;
};

const PositionDetailsSideSheet: React.FC<PositionDetailsSideSheetProps> = ({ positions }) => {
    const { currentPosition, setCurrentPosition } = useCurrentPosition(positions);
    const [activeTabKey, setActiveTabKey] = React.useState<string>('pro-org');
    const [isEditingRoleDescription, setIsEditingRoleDescription] = React.useState<boolean>(false);

    const showSideSheet = React.useMemo(() => currentPosition !== null, [currentPosition]);

    const onClose = React.useCallback(() => {
        setCurrentPosition(null);
    }, [setCurrentPosition]);

    const selectedInstance = React.useMemo(() => currentPosition?.instances[0], [currentPosition]);

    const filterToDate = React.useMemo(() => new Date(), []);

    return (
        <ModalSideSheet
            show={showSideSheet}
            onClose={onClose}
            header={currentPosition?.name || 'TBN'}
            safeClose={isEditingRoleDescription}
            safeCloseTitle="There are unsaved changes"
            safeCloseCancelLabel="Continue editing"
            safeCloseConfirmLabel="Discard current changes"
        >
            {currentPosition === null ? null : (
                <div className={styles.container}>
                    <header>
                        <h1>{currentPosition.basePosition.name}</h1>
                        <div className={styles.nameWithIcon}>
                            <h2>{currentPosition.name}</h2>
                        </div>
                        <h1>
                            {selectedInstance ? formatDate(selectedInstance.appliesFrom) : ''} -{' '}
                            {selectedInstance ? formatDate(selectedInstance.appliesTo) : ''}
                        </h1>
                        <h3>Currently assigned to</h3>

                        <PositionAssignment instance={selectedInstance} />
                    </header>
                    <div className={styles.tabsWrapper}>
                        <Tabs activeTabKey={activeTabKey} onChange={key => setActiveTabKey(key)}>
                            <Tab tabKey="pro-org" title="ProOrganisation">
                                <div className={styles.tabContent}>
                                    <ReportingPathTab
                                        filterToDate={filterToDate}
                                        selectedPosition={currentPosition}
                                    />
                                </div>
                            </Tab>
                            <Tab tabKey="position-timeline" title="Position timeline">
                                <div className={styles.tabContent}>
                                    <PositionTimeline
                                        selectedDate={filterToDate}
                                        selectedPosition={currentPosition}
                                    />
                                </div>
                            </Tab>
                            <Tab tabKey="discipline-network" title="Contract discipline network">
                                <div className={styles.tabContent}>
                                    <DisciplineNetworkTab
                                        filterToDate={filterToDate}
                                        selectedPosition={currentPosition}
                                        positions={positions || []}
                                    />
                                </div>
                            </Tab>
                            <Tab tabKey="role-description" title="Role description">
                                <div className={styles.tabContent}>
                                    <RoleDescriptionTab
                                        filterToDate={filterToDate}
                                        selectedPosition={currentPosition}
                                        onEditChange={editing =>
                                            setIsEditingRoleDescription(editing)
                                        }
                                    />
                                </div>
                            </Tab>
                        </Tabs>
                    </div>
                </div>
            )}
        </ModalSideSheet>
    );
};

export default PositionDetailsSideSheet;
