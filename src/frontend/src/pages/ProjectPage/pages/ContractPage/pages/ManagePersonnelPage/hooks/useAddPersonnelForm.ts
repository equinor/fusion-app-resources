import { useCallback } from 'react';
import Person from '../../../../../../../models/Person';
import useForm from '../../../../../../../hooks/useForm';
import * as uuid from "uuid/v1";

const createDefaultState = (): Person[] => ([
  {
    personnelId:uuid(), 
    name:"",
    phoneNumber:"",
    mail:"",
    jobTitle:""
  }]);

const useAddPersonnelForm = (defaultState?: Person[] | null) => {
    const validateForm = useCallback((formState: Person[]) => {
        return formState
        .find(p => 
          Boolean(
            p.name &&
            p.phoneNumber &&
            p.mail) === false) 
              ? false: true
    }, []);

    return useForm(createDefaultState, validateForm, defaultState);
};

export default useAddPersonnelForm;
