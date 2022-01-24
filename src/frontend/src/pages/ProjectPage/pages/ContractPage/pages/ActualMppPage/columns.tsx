import {
    DataTableColumn,
    PersonCard,
    useTooltipRef,
    WarningIcon,
} from '@equinor/fusion-components';
import PositionColumn from '../../../../components/PositionColumn';
import { formatDate, Position, useHistory } from '@equinor/fusion';
import styles from './styles.less';
import { FC, useCallback } from 'react';
import classNames from 'classnames';
import AzureAdStatusIndicator from '../../components/AzureAdStatusIndicator';
import PositionWithPersonnel from '../../../../../../models/PositionWithPersonnel';

type AssignedPersonProps = {
    item: Position;
};

type ToDateProp = {
    appliesTo: Date | undefined;
};

type AzureAdColumnProps = {
    item: PositionWithPersonnel;
};

const AssignedPersonComponent: FC<AssignedPersonProps> = ({ item }) => {
    const person = item.instances.find((i) => i.assignedPerson)?.assignedPerson || undefined;
    return <PersonCard person={person} photoSize="medium" inline />;
};

type ColumnSideSheetLinkProps = {
    positionId: string;
};

const ColumnSideSheetLink: FC<ColumnSideSheetLinkProps> = ({ positionId, children }) => {
    const history = useHistory();

    const openSideSheet = useCallback(() => {
        const sideSheetSearchString = `positionId=${positionId}`;
        history.push({
            pathname: history.location.pathname,
            search: sideSheetSearchString,
        });
    }, [history.location.pathname, positionId]);

    return (
        <div onClick={openSideSheet} className={styles.columnLink}>
            {children}
        </div>
    );
};

const ToDateComponent: FC<ToDateProp> = ({ appliesTo }) => {
    const today = new Date();
    const isOverdue = appliesTo && appliesTo.getTime() < today.getTime();

    const isSoonDue =
        appliesTo && new Date(today.setMonth(today.getMonth() + 1)).getTime() > appliesTo.getTime();

    const tooltipContent = isOverdue ? 'Position is overdue' : `Position is soon due`;
    const tooltipRef = useTooltipRef(tooltipContent, 'left');

    if (!appliesTo) {
        return <span>No date</span>;
    }

    const appliesToClasses = classNames(styles.appliesTo, {
        [styles.isSoonDue]: isSoonDue,
        [styles.isOverdue]: isOverdue,
    });
    return (
        <div className={appliesToClasses}>
            <span className={styles.date}>{formatDate(appliesTo)}</span>
            {(isOverdue || isSoonDue) && (
                <div className={styles.icon} ref={tooltipRef}>
                    <WarningIcon outline={false} />
                </div>
            )}
        </div>
    );
};

const AzureAdColumn: FC<AzureAdColumnProps> = ({ item }) => {
    const personAdStatus = item.instances.find((i) => i.personnelDetails?.azureAdStatus)
        ?.personnelDetails?.azureAdStatus;

    return <AzureAdStatusIndicator status={personAdStatus || 'NoAccount'} />;
};

const columns: DataTableColumn<PositionWithPersonnel>[] = [
    {
        accessor: (position) => position.name || 'TBN',
        key: 'position',
        label: 'Position',
        sortable: true,
        component: ({ item }) => (
            <ColumnSideSheetLink positionId={item.id}>{item.name || 'TBN'}</ColumnSideSheetLink>
        ),
    },
    {
        accessor: (position) =>
            position.instances.find((i) => i.assignedPerson?.name)?.assignedPerson?.name || '',
        key: 'person',
        label: 'Person',
        sortable: true,
        component: AssignedPersonComponent,
    },
    {
        accessor: (position) =>
            position.instances.find((i) => i.personnelDetails?.azureAdStatus)?.personnelDetails
                ?.azureAdStatus || '',
        key: 'adStatus',
        label: 'Person AD Status',
        sortable: true,
        component: AzureAdColumn,
    },
    {
        accessor: (position) =>
            position.instances.find((i) => !isNaN(i.workload))?.workload.toString() + '%' || '',
        key: 'workload',
        label: 'Workload',
        sortable: true,
        component: ({ item }) => (
            <ColumnSideSheetLink positionId={item.id}>
                {item.instances.find((i) => !isNaN(i.workload))?.workload.toString() + '%' || ''}
            </ColumnSideSheetLink>
        ),
    },
    {
        accessor: (position) =>
            position.instances
                .find((i) => i.appliesTo)
                ?.appliesTo.getTime()
                .toString() || '0',
        key: 'to-date',
        label: 'To date',
        sortable: true,
        component: ({ item }) => (
            <ColumnSideSheetLink positionId={item.id}>
                <ToDateComponent appliesTo={item.instances.find((i) => i.appliesTo)?.appliesTo} />
            </ColumnSideSheetLink>
        ),
    },
    {
        accessor: (position) => position.basePosition?.discipline || 'TBN',
        key: 'discipline',
        label: 'Discipline',
        sortable: true,
        component: ({ item }) => (
            <ColumnSideSheetLink positionId={item.id}>
                {item.basePosition?.discipline || 'TBN'}
            </ColumnSideSheetLink>
        ),
    },

    {
        accessor: (position) =>
            position.instances.find((i) => i.parentPositionId)?.parentPositionId || '',
        key: 'taskOwnerId',
        label: 'Task owner',
        sortable: true,
        component: ({ item }) => {
            const taskOwnerId =
                item.instances.find((i) => i.parentPositionId)?.parentPositionId || null;
            return <PositionColumn positionId={taskOwnerId} />;
        },
    },
];

export default columns;
