import * as React from 'react';
import { ModalSideSheet, Button, Spinner } from '@equinor/fusion-components';
import Personnel from '../../../../../../../models/Personnel';
import Person from '../../../../../../../models/Person';
import * as uuid from "uuid/v1";
import * as classNames from 'classnames'
import * as styles from './styles.less'
import { useComponentDisplayClassNames, useCurrentContext } from '@equinor/fusion';
import { generateRowTemplate, generateColumnTemplate } from './utils';
import Header from './AddPersonnelFormHeader';
import { useAppContext } from '../../../../../../../appContext';
import { useContractContext } from '../../../../../../../contractContex';
import AddPersonnelFormTextInput from './AddPersonnelFormTextInput';
import useAddPersonnelForm from '../hooks/useAddPersonnelForm';


type AddPersonnelToSideSheetProps = {
  isOpen: boolean;
  selectedPersonnel:Personnel[] | null
  setIsOpen: (state:boolean) => void;
}


const AddPersonnelSideSheet: React.FC<AddPersonnelToSideSheetProps> = ({isOpen,setIsOpen,selectedPersonnel}) => {
  const currentContext = useCurrentContext()
  const currentContract = useContractContext()
  const {
    formState,
    setFormState,
    resetForm,
    isFormValid,
    isFormDirty,
  } =  useAddPersonnelForm(selectedPersonnel)
  const { apiClient } = useAppContext();
  
  const [saveInProgress,setSaveInProgress] = React.useState<Boolean>(false);
  
  const  savePersonnelChangesAsync = async () => {
    const contractId = currentContract.contract?.id;


    if(!currentContext?.id || !contractId )
      return  
    
    setSaveInProgress(true);
    await Promise.all(formState
      .map(async person => await apiClient.updatePersonnelAsync(currentContext.id,contractId,person)))
      .then(() => {
        setSaveInProgress(false)
        setIsOpen(false);
      })
      .catch(() => setSaveInProgress(false))  
  }

  const onChange = React.useCallback((changedPerson: Person)=>{
    const updatedPersons = formState.map((p) => p.personnelId === changedPerson.personnelId ? changedPerson:p);
    setFormState(updatedPersons);
  },[formState,setFormState])

  const headers = ["Name","Mail","Phone"]
  const rowTemplate = generateRowTemplate(headers);
  const columnTemplate = generateColumnTemplate(headers);
  const containerClassNames = classNames(styles.container, useComponentDisplayClassNames(styles));
  
  return(
    <ModalSideSheet
      header="Add Person"
      show={isOpen}
      onClose={() => {
        setIsOpen(false);
      }}
      safeClose
      safeCloseTitle={`Close Add Person? Unsaved changes will be lost.`}
      headerIcons={[
        <Button key={"AddPerson"} outlined onClick = {()=> {setFormState([...formState,{personnelId:uuid(), name:"",phoneNumber:"",mail:"",jobTitle:""}])}} > + Add Person </Button>,
        <Button disabled={!(isFormDirty && isFormValid)} key={"save"} outlined onClick = {()=> {savePersonnelChangesAsync()} }> Create </Button>
      ]}
      isResizable
      minWidth={640}
    >
      {saveInProgress && <Spinner centered  size={100} title={"Saving Personnel"} />  }
      {!saveInProgress && isOpen &&  <div className={containerClassNames}>
        <div className={styles.table}
            style={{ gridTemplateColumns: columnTemplate, gridTemplateRows: rowTemplate}}
        >
          <Header headers={headers} />
          <table>
            <tbody>
              {
                formState.map((person) => (
                  <tr key={`person${person.personnelId}`} style={{marginBottom:"16px", display:"flex"}}>
                    <td style={{flexGrow:1}}><AddPersonnelFormTextInput key={`name${person.personnelId}`} item={person} onChange={onChange} field={"name"}/></td>
                    <td style={{flexGrow:1}}><AddPersonnelFormTextInput key={`mail${person.personnelId}`} item={person} onChange={onChange} field={"mail"}/></td>
                    <td style={{flexGrow:1}}><AddPersonnelFormTextInput key={`phoneNumber${person.personnelId}`} item={person} onChange={onChange} field={"phoneNumber"}/></td>
                  </tr>
                ))
              }
            </tbody>
          </table>
        </div>
      </div>}
      
    </ModalSideSheet>
  )
}

export default AddPersonnelSideSheet



