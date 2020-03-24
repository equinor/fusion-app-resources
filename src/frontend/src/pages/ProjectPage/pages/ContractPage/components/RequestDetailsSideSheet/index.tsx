import * as React from 'react';
import PersonnelRequest from '../../../../../../models/PersonnelRequest';
import {
    ModalSideSheet,
    Tabs,
    Tab,
    Accordion,
    AccordionItem,
    ErrorMessage,
    Button,
    CloseCircleIcon,
    styling,
    CheckCircleIcon,
    Spinner,
} from '@equinor/fusion-components';
import RequestDetails from './RequestDetails';
import useCurrentRequest from './hooks/useCurrentRequest';
import RequestWorkflow from '../RequestWorkflow';
import * as styles from './styles.less';
import CompactPersonDetails from './CompactPersonDetails';
import useRequestApproval from '../../hooks/useRequestApproval';
import RejectPersonnelSideSheet from '../RejectRequestSideSheet';
import useRequestRejection from '../../hooks/useRequestRejection';
import EditablePositionDetails from '../EditablePositionDetails';
import PersonPositionsDetails from '../PersonPositionsDetails';

type RequestDetailsSideSheetProps = {
    requests: PersonnelRequest[] | null;
};
type AccordionOpenDictionary = {
    description: boolean;
    person: boolean;
    comments: boolean;
};

const RequestDetailsSideSheet: React.FC<RequestDetailsSideSheetProps> = ({ requests }) => {
    const { currentRequest, setCurrentRequest } = useCurrentRequest(requests);
    const [activeTabKey, setActiveTabKey] = React.useState<string>('general');
    const [rejectRequest, setRejectRequest] = React.useState<PersonnelRequest[]>([]);
    const [openAccordions, setOpenAccordions] = React.useState<AccordionOpenDictionary>({
        comments: true,
        description: true,
        person: true,
    });
    const { approve, canApprove, isApproving } = useRequestApproval(
        currentRequest ? [currentRequest] : []
    );
    const { reject, canReject, isRejecting } = useRequestRejection(
        currentRequest ? [currentRequest] : []
    );
    const showSideSheet = React.useMemo(() => currentRequest !== null, [currentRequest]);

    const onClose = React.useCallback(() => {
        setCurrentRequest(null);
    }, [setCurrentRequest]);

    const handleAccordionStateChange = React.useCallback(
        (id: keyof AccordionOpenDictionary) => {
            setOpenAccordions({ ...openAccordions, [id]: !openAccordions[id] });
        },
        [setOpenAccordions, openAccordions]
    );

    if (!currentRequest) {
        return null;
    }

    return (
        <ModalSideSheet
            show={showSideSheet}
            header={currentRequest.position?.basePosition?.name || ''}
            onClose={onClose}
            headerIcons={[
                <Button
                    outlined
                    disabled={!canReject}
                    onClick={() => canReject && setRejectRequest([currentRequest])}
                >
                    <div className={styles.buttonIcon}>
                        {isRejecting ? (
                            <Spinner inline />
                        ) : (
                            <CloseCircleIcon
                                width={styling.numericalGrid(2)}
                                height={styling.numericalGrid(2)}
                            />
                        )}
                    </div>
                    Reject
                </Button>,
                <Button disabled={!canApprove} onClick={() => canApprove && approve()}>
                    <div className={styles.buttonIcon}>
                        {isApproving ? (
                            <Spinner inline />
                        ) : (
                            <CheckCircleIcon
                                width={styling.numericalGrid(2)}
                                height={styling.numericalGrid(2)}
                            />
                        )}
                    </div>
                    Approve
                </Button>,
            ]}
        >
            <Tabs activeTabKey={activeTabKey} onChange={setActiveTabKey}>
                <Tab tabKey="general" title="General">
                    <div className={styles.tabContainer}>
                        <div className={styles.container}>
                            <RequestWorkflow workflow={currentRequest.workflow} />
                        </div>
                        <div className={styles.separator} />
                        <div className={styles.container}>
                            <Accordion>
                                <AccordionItem
                                    label="Description"
                                    onChange={() => handleAccordionStateChange('description')}
                                    key="description"
                                    isOpen={openAccordions.description}
                                >
                                    <RequestDetails request={currentRequest} />
                                </AccordionItem>
                                <AccordionItem
                                    label="Person"
                                    onChange={() => handleAccordionStateChange('person')}
                                    key="person"
                                    isOpen={openAccordions.person}
                                >
                                    {currentRequest.person ? (
                                        <>
                                            <CompactPersonDetails
                                                personnel={currentRequest.person}
                                            />
                                            <PersonPositionsDetails
                                                person={currentRequest.person}
                                            />
                                        </>
                                    ) : (
                                        <ErrorMessage
                                            hasError
                                            errorType="noData"
                                            message="Could not find a person for this request"
                                        />
                                    )}
                                </AccordionItem>
                                <AccordionItem
                                    label="Comments"
                                    onChange={() => handleAccordionStateChange('comments')}
                                    key="comments"
                                    isOpen={openAccordions.comments}
                                >
                                    <div>No comments</div>
                                </AccordionItem>
                            </Accordion>
                        </div>
                    </div>
                </Tab>
                <Tab tabKey="description" title="Description">
                    <div className={styles.tabContainer}>
                        <div className={styles.container}>
                            <RequestDetails request={currentRequest} />
                        </div>
                    </div>
                </Tab>
                <Tab tabKey="person" title="Person">
                    <div className={styles.tabContainer}>
                        <div className={styles.container}>
                            {currentRequest.person ? (
                                <EditablePositionDetails person={currentRequest.person} />
                            ) : (
                                <ErrorMessage hasError title="No person assigned" />
                            )}
                        </div>
                    </div>
                </Tab>
            </Tabs>
            <RejectPersonnelSideSheet
                requests={rejectRequest}
                setRequests={setRejectRequest}
                onReject={reason => reject(reason)}
            />
        </ModalSideSheet>
    );
};
export default RequestDetailsSideSheet;
