
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
import styles from './styles.less';
import CompactPersonDetails from './CompactPersonDetails';
import useRequestApproval from '../../hooks/useRequestApproval';
import RejectPersonnelSideSheet from '../RejectRequestSideSheet';
import useRequestRejection from '../../hooks/useRequestRejection';
import EditablePositionDetails from '../EditablePositionDetails';
import PersonPositionsDetails from '../PersonPositionsDetails';
import usePersonnel from '../../pages/ManagePersonnelPage/hooks/usePersonnel';
import * as moment from 'moment';
import classNames from 'classnames';
import { FC, useState, useCallback, useMemo } from 'react';

type RequestDetailsSideSheetProps = {
    requests: PersonnelRequest[] | null;
};
type AccordionOpenDictionary = {
    description: boolean;
    person: boolean;
    comments: boolean;
};

const RequestDetailsSideSheet: FC<RequestDetailsSideSheetProps> = ({ requests }) => {
    const { currentRequest, setCurrentRequest } = useCurrentRequest(requests);
    const [activeTabKey, setActiveTabKey] = useState<string>('general');
    const [rejectRequest, setRejectRequest] = useState<PersonnelRequest[]>([]);
    const [openAccordions, setOpenAccordions] = useState<AccordionOpenDictionary>({
        comments: true,
        description: true,
        person: true,
    });

    const onClose = useCallback(() => {
        setCurrentRequest(null);
    }, [setCurrentRequest]);

    const { approve, canApprove, isApproving } = useRequestApproval(
        currentRequest ? [currentRequest] : [],
        onClose
    );
    const { reject, canReject, isRejecting } = useRequestRejection(
        currentRequest ? [currentRequest] : [],
        onClose
    );
    const showSideSheet = useMemo(() => currentRequest !== null, [currentRequest]);

    const handleAccordionStateChange = useCallback(
        (id: keyof AccordionOpenDictionary) => {
            setOpenAccordions({ ...openAccordions, [id]: !openAccordions[id] });
        },
        [setOpenAccordions, openAccordions]
    );

    const { personnel } = usePersonnel();
    const originalPersonnel = personnel.find(
        (p) => p.mail === currentRequest?.originalPerson?.mail
    );

    const currentPerson = useMemo(() => {
        const personnelPerson = personnel.find(
            (p) => p.azureUniquePersonId === currentRequest?.person?.azureUniquePersonId
        );
        return personnelPerson || currentRequest?.person;
    }, [currentRequest, personnel]);

    const isRequestCompleted = useMemo(
        () => !!(currentRequest?.state === 'ApprovedByCompany'),
        [currentRequest]
    );

    const rejectedStep = useMemo(
        () => currentRequest?.workflow?.steps.find((s) => s.state === 'Rejected'),
        [currentRequest]
    );

    if (!currentRequest) {
        return null;
    }

    return (
        <ModalSideSheet
            show={showSideSheet}
            header={currentRequest.position?.basePosition?.name || ''}
            onClose={onClose}
            headerIcons={
                isRequestCompleted
                    ? undefined
                    : [
                          canReject && (
                              <Button
                                  outlined
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
                              </Button>
                          ),
                          canApprove && (
                              <Button onClick={() => canApprove && approve()}>
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
                              </Button>
                          ),
                      ]
            }
        >
            <Tabs activeTabKey={activeTabKey} onChange={setActiveTabKey}>
                <Tab tabKey="general" title="General">
                    <div className={styles.tabContainer}>
                        <div className={styles.container}>
                            {currentRequest.workflow && currentRequest.provisioningStatus && (
                                <RequestWorkflow
                                    workflow={currentRequest.workflow}
                                    provisioningStatus={currentRequest.provisioningStatus}
                                />
                            )}
                        </div>
                        {rejectedStep ? (
                            <div
                                className={classNames(
                                    styles.container,
                                    styles.rejectedReasonContainer
                                )}
                            >
                                <h3>
                                    Rejected{' '}
                                    {rejectedStep.completed
                                        ? moment(rejectedStep.completed).fromNow()
                                        : ''}{' '}
                                    by {rejectedStep.completedBy?.name}
                                </h3>
                                <div className={styles.rejectedReason}>
                                    <h6>Reason</h6>
                                    <p>{rejectedStep.reason}</p>
                                </div>
                            </div>
                        ) : null}
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
                                        <CompactPersonDetails
                                            personnel={currentRequest.person}
                                            originalPersonnel={originalPersonnel}
                                        />
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
                            {currentPerson ? (
                                <>
                                    <EditablePositionDetails person={currentPerson} />
                                    <PersonPositionsDetails person={currentPerson} />
                                </>
                            ) : (
                                <ErrorMessage
                                    hasError
                                    errorType="noData"
                                    message="Could not find a person for this request"
                                />
                            )}
                        </div>
                    </div>
                </Tab>
            </Tabs>
            <RejectPersonnelSideSheet
                requests={rejectRequest}
                setRequests={setRejectRequest}
                onReject={(reason) => reject(reason)}
            />
        </ModalSideSheet>
    );
};
export default RequestDetailsSideSheet;
