
import styles from './styles.less';
import CertifyToPicker from '../../../CertifiyToPicker';
import {
    Button,
    SyncIcon,
    Spinner,
    useDropdownController,
    Dropdown,
} from '@equinor/fusion-components';
import PersonDelegation from '../../../../../../../../models/PersonDelegation';
import { useAppContext } from '../../../../../../../../appContext';
import { useContractContext } from '../../../../../../../../contractContex';
import { useCurrentContext, useNotificationCenter } from '@equinor/fusion';
import ToolbarButton from '../ToolbarButton';
import { FC, useState, useCallback } from 'react';

type CertifyToPopoverProps = {
    canEdit: boolean;
    admins: PersonDelegation[];
};

const CertifyToPopover: FC<CertifyToPopoverProps> = ({ canEdit, admins, children }) => {
    const [selectedDate, setSelectedDate] = useState<Date | null>(null);

    const [isReCertifying, setIsReCertifying] = useState<boolean>(false);
    const [reCertificationError, setReCertificationError] = useState<Error | null>(null);
    const { apiClient } = useAppContext();
    const { dispatchContractAction, contract } = useContractContext();
    const currentContext = useCurrentContext();
    const sendNotification = useNotificationCenter();

    const controller = useDropdownController((ref, isOpen, setIsOpen) => (
        <ToolbarButton
            id="recertify-btn"
            ref={ref}
            onClick={() => setIsOpen(!isOpen)}
            icon={<SyncIcon />}
            title="Re-certify"
            disabled={!canEdit || admins.length <= 0}
        />
    ));

    const reCertifyAdminsAsync = useCallback(
        async (projectId: string, contractId: string, date: Date) => {
            setIsReCertifying(true);
            setReCertificationError(null);

            try {
                const requests = admins.map(async (a) =>
                    apiClient.reCertifyRoleDelegationAsync(projectId, contractId, a.id, date)
                );
                const response = await Promise.all(requests);
                dispatchContractAction({
                    collection: 'administrators',
                    verb: 'merge',
                    payload: response,
                });
            } catch (e) {
                setReCertificationError(e);
                sendNotification({
                    level: 'high',
                    title: 'Unable to re-certify new person(s)',
                    body: e?.response?.error?.message || ""
                });
            } finally {
                setIsReCertifying(false);
            }
        },
        [admins, apiClient]
    );

    const onReCertifyClick = useCallback(() => {
        const contractId = contract?.id;
        const projectId = currentContext?.id;

        if (contractId && projectId && selectedDate && canEdit) {
            reCertifyAdminsAsync(projectId, contractId, selectedDate);
        }
    }, [currentContext, contract, canEdit, reCertifyAdminsAsync, selectedDate]);

    return (
        <Dropdown controller={controller}>
            <div data-cy="recertify-popup" className={styles.container}>
                <CertifyToPicker
                    onChange={setSelectedDate}
                    defaultSelected="12-months"
                    isReCertification
                />
                <div className={styles.certifyButtonContainer}>
                    <Button id="recertify-btn" disabled={!canEdit} onClick={onReCertifyClick}>
                        <div className={styles.syncButton}>
                            {isReCertifying ? (
                                <Spinner inline />
                            ) : (
                                <>
                                    <SyncIcon />
                                    <span>Re-Certify</span>
                                </>
                            )}
                        </div>
                    </Button>
                </div>
            </div>
        </Dropdown>
    );
};

export default CertifyToPopover;
