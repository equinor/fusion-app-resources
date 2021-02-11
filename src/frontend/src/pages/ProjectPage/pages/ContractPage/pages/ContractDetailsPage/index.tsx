
import { useContractContext } from '../../../../../../contractContex';
import styles from './styles.less';
import {
    SkeletonBar,
    PositionCard,
    SkeletonDisc,
    IconButton,
    EditIcon,
    useTooltipRef,
    HelpIcon,
} from '@equinor/fusion-components';
import { formatDate, Position, useHistory, useCurrentContext } from '@equinor/fusion';
import Contract from '../../../../../../models/contract';
import { getInstances, isInstanceFuture, isInstancePast } from '../../../../orgHelpers';
import { Link } from 'react-router-dom';
import ContractAdminTable from '../../components/ContractAdminTable';
import { useAppContext } from '../../../../../../appContext';
import useReducerCollection from '../../../../../../hooks/useReducerCollection';
import { ReactNode, useMemo, useCallback } from 'react';

const createFieldWithSkeleton = (
    name: string,
    render: (contract: Contract) => ReactNode,
    renderSkeleton?: () => ReactNode
) => {
    const { isFetchingContract, contract } = useContractContext();

    return (
        <div className={styles.field}>
            <label>{name}</label>
            <div className={styles.value}>
                {isFetchingContract && !contract ? (
                    renderSkeleton ? (
                        renderSkeleton()
                    ) : (
                        <SkeletonBar />
                    )
                ) : contract ? (
                    render(contract)
                ) : null}
            </div>
        </div>
    );
};

const ContractNumber = () =>
    createFieldWithSkeleton('Contract number', (contract) => contract.contractNumber);
const Contractor = () =>
    createFieldWithSkeleton('Contractor', (contract) => contract.company?.name || null);
const FromDate = () =>
    createFieldWithSkeleton('From date', (contract) =>
        contract.startDate ? formatDate(contract.startDate) : 'N/A'
    );
const ToDate = () =>
    createFieldWithSkeleton('To date', (contract) =>
        contract.endDate ? formatDate(contract.endDate) : 'N/A'
    );

const PositionCardSkeleton = () => (
    <div className={styles.positionCardSkeleton}>
        <SkeletonDisc size="medium" />
        <div className={styles.content}>
            <SkeletonBar />
            <SkeletonBar />
        </div>
    </div>
);

const DelegateAdminTitle = () => (
    <div className={styles.field}>
        <label>Delegate admin access</label>
    </div>
);

const renderPosition = (position: Position | null) => {
    if (!position) {
        return 'N/A';
    }
    const filterToDate = useMemo(() => new Date(), []);
    const instance = useMemo(() => getInstances(position, filterToDate)[0], [
        position,
        filterToDate,
    ]);
    const isFuture = useMemo(() => isInstanceFuture(instance, filterToDate), [
        position,
        filterToDate,
    ]);
    const isPast = useMemo(() => isInstancePast(instance, filterToDate), [
        position,
        filterToDate,
    ]);
    return (
        <PositionCard
            position={position}
            instance={instance}
            showDate
            showExternalId
            showLocation
            showObs
            showTimeline
            showRotation
            isFuture={isFuture}
            isPast={isPast}
        />
    );
};

const EquinorContractResponsible = () =>
    createFieldWithSkeleton(
        'Equinor contract responsible',
        (contract) => renderPosition(contract.contractResponsible),
        () => <PositionCardSkeleton />
    );
const EquinorCompanyRep = () =>
    createFieldWithSkeleton(
        'Equinor company rep',
        (contract) => renderPosition(contract.companyRep),
        () => <PositionCardSkeleton />
    );
const ExternalCompanyRep = () =>
    createFieldWithSkeleton(
        'External company rep',
        (contract) => renderPosition(contract.externalCompanyRep),
        () => <PositionCardSkeleton />
    );
const ExternalContractResponsible = () =>
    createFieldWithSkeleton(
        'External contract responsible',
        (contract) => renderPosition(contract.externalContractResponsible),
        () => <PositionCardSkeleton />
    );

const ContractDetailsPage = () => {
    const editTooltipRef = useTooltipRef('Edit contract', 'left');
    const helpIconRef = useTooltipRef('Help page', 'left');

    const history = useHistory();
    const { contract, contractState, dispatchContractAction } = useContractContext();
    const currentContext = useCurrentContext();
    const { apiClient } = useAppContext();

    const fetchRequestsAsync = useCallback(async () => {
        const contractId = contract?.id;
        const projectId = currentContext?.id;
        if (!contractId || !projectId) {
            return [];
        }

        return await apiClient.getPersonDelegationsAsync(projectId, contractId);
    }, [contract, currentContext, apiClient]);
    const { data, isFetching, error } = useReducerCollection(
        contractState,
        dispatchContractAction,
        'administrators',
        fetchRequestsAsync,
        'set'
    );

    const crAdministrators = useMemo(() => data.filter((d) => d.type === 'CR'), [data]);

    const internalAdministrators = useMemo(
        () => crAdministrators.filter((d) => d.classification === 'Internal'),
        [crAdministrators]
    );
    const externalAdministrators = useMemo(
        () => crAdministrators.filter((d) => d.classification === 'External'),
        [crAdministrators]
    );

    return (
        <div className={styles.container}>
            <div className={styles.contractDetails}>
                <div className={styles.header}>Contract details</div>
                <div className={styles.row}>
                    <ContractNumber />
                    <Contractor />
                </div>
                <div className={styles.row}>
                    <FromDate />
                    <ToDate />
                </div>
                <div className={styles.header}>Equinor responsible</div>
                <div className={styles.row}>
                    <EquinorCompanyRep />
                    <EquinorContractResponsible />
                </div>
                <DelegateAdminTitle />

                <div className={styles.row}>
                    <ContractAdminTable
                        accountType="Internal"
                        admins={internalAdministrators}
                        isFetchingAdmins={isFetching}
                    />
                </div>
                <div className={styles.header}>External responsible</div>
                <div className={styles.row}>
                    <ExternalCompanyRep />
                    <ExternalContractResponsible />
                </div>
                <DelegateAdminTitle />
                <div className={styles.row}>
                    <ContractAdminTable
                        accountType="External"
                        admins={externalAdministrators}
                        isFetchingAdmins={isFetching}
                    />
                </div>
            </div>
            <div className={styles.aside}>
                <IconButton
                    ref={editTooltipRef}
                    onClick={() => history.push(`/${currentContext?.id}/${contract?.id}/edit`)}
                >
                    <EditIcon />
                </IconButton>
                <Link target="_blank" to="/help">
                    <IconButton ref={helpIconRef}>
                        <HelpIcon />
                    </IconButton>
                </Link>
            </div>
        </div>
    );
};

export default ContractDetailsPage;
